using RabbitMQ.Client;
using Confluent.Kafka;
using Google.Protobuf;
using NexusSentinel.Shared.Protos;

namespace NexusSentinel.AlertProcessor;

/// <summary>
/// Proactive monitoring service that analyzes the telemetry stream in real-time.
/// It detects anomalies based on configurable thresholds and triggers critical alerts 
/// via RabbitMQ for immediate visibility.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private IConnection? _rabbitConnection;
    private IChannel? _rabbitChannel;
    private readonly double _temperatureThreshold;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        _temperatureThreshold = Convert.ToDouble(_config["AppLimits:ThresholdValue"] ?? "100.0");
    }

    /// <summary>
    /// Lifecycle of the Alert Processor. Initializes infrastructure and starts the analysis loop.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Setup the reliable messaging path first
        await InitializeRabbitMQAsync();

        var kafkaConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = _config["Kafka:GroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        // Consuming binary data to match the high-performance pipeline
        using var consumer = new ConsumerBuilder<Ignore, byte[]>(kafkaConfig).Build();
        consumer.Subscribe(_config["Kafka:Topic"] ?? "telemetry");

        _logger.LogInformation("AlertProcessor Active. Threshold set to: {Threshold}°C", _temperatureThreshold);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                
                if (result?.Message?.Value != null)
                {
                    /*
                     * REFACTOR: Parse from Binary Protobuf
                     * Eliminates the need for temporary JSON conversion during analysis.
                     */
                    var record = TelemetryRecord.Parser.ParseFrom(result.Message.Value);

                    // Business Logic: Threshold violation check
                    if (record.Temperature > _temperatureThreshold)
                    {
                        await GenerateAndPublishAlertAsync(record);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AlertProcessor analysis loop stopped.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure in AlertProcessor worker.");
        }
        finally
        {
            consumer.Close();
            if (_rabbitConnection != null) await _rabbitConnection.CloseAsync();
        }
    }

    /// <summary>
    /// Configures RabbitMQ infrastructure (Exchange/Queue/Binding) for reliable alert routing.
    /// </summary>
    private async Task InitializeRabbitMQAsync()
    {
        var factory = new ConnectionFactory { HostName = _config["RabbitMQ:HostName"] ?? "localhost" };
        _rabbitConnection = await factory.CreateConnectionAsync();
        _rabbitChannel = await _rabbitConnection.CreateChannelAsync();
        
        // Define a Fanout exchange to allow broadcasting alerts to multiple consumers (e.g., Dashboard, Logs)
        string exchange = _config["RabbitMQ:ExchangeName"] ?? "alerts-exchange";
        string queue = _config["RabbitMQ:QueueName"] ?? "alerts-queue";

        await _rabbitChannel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout);
        await _rabbitChannel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
        await _rabbitChannel.QueueBindAsync(queue, exchange, routingKey: "");
    }

    /// <summary>
    /// Constructs a professional AlertMessage and publishes it to the operational event bus.
    /// </summary>
    private async Task GenerateAndPublishAlertAsync(TelemetryRecord record)
    {
        var alert = new AlertMessage
        {
            DeviceId = record.DeviceId,
            AlertType = "HighTemperature",
            CurrentValue = record.Temperature,
            ThresholdValue = _temperatureThreshold,
            Severity = "Critical",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // Protobuf messages use ToByteArray() for efficient serialization over the wire
        ReadOnlyMemory<byte> body = alert.ToByteArray();

        await _rabbitChannel!.BasicPublishAsync(
            exchange: _config["RabbitMQ:ExchangeName"] ?? "alerts-exchange",
            routingKey: "",
            body: body);

        _logger.LogWarning("[ALERT TRIGGERED] Device {Id} reported {Val}°C! (Threshold: {T})", 
            alert.DeviceId, alert.CurrentValue, alert.ThresholdValue);
    }
}
