using Confluent.Kafka;
using NexusSentinel.Shared.Protos;
using StackExchange.Redis;
using Google.Protobuf;
using System.Text.Json;

namespace NexusSentinel.Processor;

/// <summary>
/// Background worker responsible for maintaining the "Hot Path" (Current State) of devices.
/// It consumes binary telemetry events from Kafka, parses them via Protobuf, 
/// and updates the real-time cache in Redis.
/// </summary>
public class Worker(ILogger<Worker> logger, IConfiguration configuration, IConnectionMultiplexer redis) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly IDatabase _redisDb = redis.GetDatabase();

    /// <summary>
    /// Core execution loop for the Kafka consumer.
    /// Implements graceful shutdown and commit-on-success patterns.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = _configuration["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false // Manual commit for at-least-once delivery guarantees
        };

        // Building consumer with <string, byte[]> to match the refined producer
        using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();

        var topic = _configuration["Kafka:Topic"] ?? "telemetry";
        consumer.Subscribe(topic);

        _logger.LogInformation("Processor initialized. Syncing 'telemetry' stream to Redis.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Blocking call to consume messages with a timeout/cancellation hook
                    var result = consumer.Consume(stoppingToken);

                    if (result?.Message?.Value != null)
                    {
                        /* 
                         * REFACTOR: Protobuf Deserialization
                         * Using the static Parser avoids the overhead of JSON reflection.
                         */
                        var telemetry = TelemetryRecord.Parser.ParseFrom(result.Message.Value);

                        _logger.LogInformation("[HOT-PATH] Syncing Device: {Id} | State: {Temp}°C, {Hum}%",
                            telemetry.DeviceId, telemetry.Temperature, telemetry.Humidity);

                        /*
                         * PERSISTENCE: Updating the Real-time Cache
                         * Even though we consume binary, we store JSON in Redis 
                         * to ensure compatibility with various dashboard technologies (Blazor, React, etc.)
                         */
                        string jsonState = JsonSerializer.Serialize(telemetry);

                        // Update current state in Redis without TTL to represent the 'last known good' status
                        await _redisDb.StringSetAsync($"device:{telemetry.DeviceId}", jsonState);

                        // Mark message as processed in Kafka offsets
                        consumer.Commit(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Consumer shutdown initiated via signal.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transient error during message processing. Attempting recovery...");
                    await Task.Delay(1000, stoppingToken); // Throttling on error
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal failure in Processor Service loop.");
        }
        finally
        {
            /*
             * GRACEFUL SHUTDOWN:
             * consumer.Close() informs the group coordinator that this member is leaving,
             * triggering an immediate rebalance for other active members.
             */
            consumer.Close();
        }
    }
}
