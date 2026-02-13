using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using Google.Protobuf;
using NexusSentinel.Shared.Protos;

namespace NexusSentinel.Persistence;

/// <summary>
/// Long-term storage worker that archives every telemetry event into Elasticsearch.
/// It implements a "Cold Path" strategy, ensuring all historical data is available 
/// for audit, trend analysis, and Kibana visualizations.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConsumer<Ignore, byte[]> _consumer;
    private readonly ElasticsearchClient _elasticsearchClient;

    public Worker(ILogger<Worker> logger, IConfiguration config, ElasticsearchClient elasticsearchClient)
    {
        _logger = logger;
        _elasticsearchClient = elasticsearchClient;

        // Initialization of the Kafka Consumer with binary payload support
        _consumer = new ConsumerBuilder<Ignore, byte[]>(new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"]
                ?? throw new ArgumentNullException("Kafka:BootstrapServers"),
            GroupId = config["Kafka:GroupId"]
                ?? "persistence-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();

        _consumer.Subscribe(config["Kafka:Topic"] ?? "telemetry");
    }

    /// <summary>
    /// Background execution loop that consumes from Kafka and indexes into Elasticsearch.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Persistence Service initialized. Archiving telemetry to Elasticsearch.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null) continue;

                try
                {
                    /*
                     * REFACTOR: Parse from Binary Protobuf
                     * Direct parsing from the wire ensures maximum efficiency for high-volume archiving.
                     */
                    var telemetry = TelemetryRecord.Parser.ParseFrom(consumeResult.Message.Value);

                    // Convert Unix Unix timestamp to DateTime for Elasticsearch compatibility
                    var dateTime = DateTimeOffset.FromUnixTimeSeconds(telemetry.Timestamp).UtcDateTime;

                    /*
                     * STORAGE STRATEGY: Daily Indexing
                     * Rationale: Creating daily indices (e.g., telemetry-2026.02.13) allows for 
                     * easier data retention management (e.g., deleting old indices) and 
                     * improves query performance by narrowing the search scope.
                     */
                    var indexName = $"telemetry-{dateTime:yyyy.MM.dd}";

                    // Map to a clean POCO/Anonymous object to avoid internal Protobuf fields in Elasticsearch
                    var document = new
                    {
                        telemetry.DeviceId,
                        telemetry.Temperature,
                        telemetry.Humidity,
                        telemetry.VibrationLevel,
                        Timestamp = dateTime
                    };

                    var response = await _elasticsearchClient.IndexAsync(document, i => i.Index(indexName), stoppingToken);

                    if (response.IsSuccess())
                    {
                        _logger.LogInformation("[COLD-PATH] Indexed record {Id} for Device {DeviceId} in {Index}",
                            response.Id, telemetry.DeviceId, indexName);
                    }
                    else
                    {
                        _logger.LogError("Failed to index record: {Error}", response.ElasticsearchServerError?.Error.Reason);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transient error during Elasticsearch indexing.");
                }
            }
        }
        finally
        {
            _consumer.Close();
            _logger.LogWarning("Persistence Service shutdown. Kafka consumer gracefully closed.");
        }
    }
}
