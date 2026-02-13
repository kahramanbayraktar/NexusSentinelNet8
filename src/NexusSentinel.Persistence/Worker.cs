using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using System.Text.Json;
using NexusSentinel.Shared.Protos;

namespace NexusSentinel.Persistence;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly ElasticsearchClient _elasticsearchClient;

    public Worker(ILogger<Worker> logger, IConfiguration config, ElasticsearchClient elasticsearchClient)
    {
        _logger = logger;
        _consumer = new ConsumerBuilder<Ignore, string>(new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"]!,
            GroupId = config["Kafka:GroupId"]!,
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();
        _consumer.Subscribe(config["Kafka:Topic"]!);

        _elasticsearchClient = elasticsearchClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null) continue;

                try
                {
                    var telemetryRecord = JsonSerializer.Deserialize<TelemetryRecord>(consumeResult.Message.Value);

                    if (telemetryRecord == null) continue;

                    Console.WriteLine($"Telemetry record received: {telemetryRecord}");

                    var dateTime = DateTimeOffset.FromUnixTimeSeconds(telemetryRecord.Timestamp).DateTime;
                    var indexName = $"telemetry-{dateTime.ToString("yyyy.MM.dd")}";

                    var document = new
                    {
                        DeviceId = telemetryRecord.DeviceId,
                        Temperature = telemetryRecord.Temperature,
                        Humidity = telemetryRecord.Humidity,
                        VibrationLevel = telemetryRecord.VibrationLevel,
                        Timestamp = dateTime
                    };

                    var response = await _elasticsearchClient.IndexAsync(document, i => i.Index(indexName), stoppingToken);
                    Console.WriteLine("--RESPONSE START--");
                    Console.WriteLine(response);
                    Console.WriteLine("--RESPONSE END--");

                    if (response.IsSuccess())
                    {
                        _logger.LogInformation("Telemetry record indexed successfully: {Id}. Index: {Index}", response.Id, indexName);
                    }
                    else
                    {
                        Console.WriteLine("Failed to index telemetry record");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Application is shutting down
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error indexing telemetry record");
                }
            }
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("Worker is stopping and Kafka Consumer is closed.");
        }
    }
}
