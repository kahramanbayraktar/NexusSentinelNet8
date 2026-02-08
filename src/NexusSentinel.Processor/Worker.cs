namespace NexusSentinel.Processor;

using System.Text.Json;
using Confluent.Kafka;
using NexusSentinel.Shared.Protos;
using StackExchange.Redis;

public class Worker(ILogger<Worker> logger, IConfiguration configuration, IConnectionMultiplexer redis) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDatabase _redisDb = redis.GetDatabase();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = _configuration["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false            
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        var topic = _configuration["Kafka:Topic"] ?? "telemetry";
        consumer.Subscribe(topic);

        _logger.LogInformation($"Kafka Consumer listening on topic: {topic}");
        
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // This inner try-catch is for handling Kafka consumer errors.
                // The outer try-catch is for handling application-level errors.
                try
                {
                    // Wait for a message from Kafka.
                    // We use cancellation token to stop the consumer gracefully.
                    // If the token is cancelled, the consumer will stop consuming messages.
                    // cancellationToken is set to true when the application is shutting down.
                    var result = consumer.Consume(stoppingToken);

                    if (result != null)
                    {
                        var json = result.Message.Value;
                        var deviceId = result.Message.Key;

                        // Deserialization
                        var telemetry = JsonSerializer.Deserialize<TelemetryRecord>(json);

                        if (telemetry != null)
                        {
                            _logger.LogInformation($"[Processing] Device: {deviceId} | Temp: {telemetry.Temperature}°C | Hum: {telemetry.Humidity}%");
                        }

                        // Best practice: Store with TTL (Time To Live) to prevent memory leaks and stale data accumulation.
                        // Memory leaks if we don't remove the data after some time.
                        // await _redisDb.StringSetAsync($"device_data:{deviceId}", json, TimeSpan.FromMinutes(5));

                        // Not fire-and-forget, but await to make sure the message is processed before moving to the next one.
                        await _redisDb.StringSetAsync($"device:{deviceId}", json);

                        // We say "I'm done with this message" to Kafka.
                        consumer.Commit(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Just logging for now, but in production we should handle this more gracefully.
                    // For example, we could use a dead-letter queue to store the failed messages.
                    _logger.LogError(ex, "Error processing message");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal Consumer Error");
        }
        finally
        {
            consumer.Close();
        }
    }
}
