using System.Threading.Tasks;
using RabbitMQ.Client;
using Confluent.Kafka;
using System.Diagnostics.Tracing;
using System;
using Google.Protobuf;
using System.Text.Json;
using NexusSentinel.Shared.Protos;

namespace NexusSentinel.AlertProcessor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private IConnection? _rabbitConnection;
    private IChannel? _rabbitChannel;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ setup (only one time outside the loop)
        await SetupRabbitMQAsync();

        // Kafka Consumer setup
        var kafkaConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"],
            GroupId = _config["Kafka:GroupId"],
            // AutoOffsetReset.Earliest means start reading from the beginning of the topic if no offset is found
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        // Ignore the key, read the value as byte array
        using var consumer = new ConsumerBuilder<Ignore, string>(kafkaConfig).Build();
        consumer.Subscribe(_config["Kafka:Topic"]);

        _logger.LogInformation("AlertProcessor started. Monitoring telemetry...");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // stoppingToken: Kafka's Consume method runs so when the applications stops, the Consume operation stops safely.
                var result = consumer.Consume(stoppingToken);
                if (result != null)
                {
                    var json = result.Message.Value;
                    var record =JsonSerializer.Deserialize<TelemetryRecord>(json);

                    if (record.Temperature > 28) // take it back to 50 when deploying to production
                    {
                        var alert = new AlertMessage
                        {
                            DeviceId = record.DeviceId,
                            AlertType = "HighTemperature",
                            CurrentValue = record.Temperature,
                            ThresholdValue = 50,
                            Severity = "Critical",
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };

                        await PublishAlertAsync(alert);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AlertProcessor global error");
        }
        finally
        {
            // Why close? It will already be disposed. Because it's a best practice to close resources when we're done with them.
            consumer.Close();

            // Why this check? Because the connection might have failed during startup.
            if (_rabbitConnection != null)
            await _rabbitConnection.CloseAsync();
        }
    }

   //  Why async?
    private async Task SetupRabbitMQAsync()
    {
        var factory = new ConnectionFactory{ HostName = _config["RabbitMQ:HostName"]! };
        _rabbitConnection = await factory.CreateConnectionAsync();
        _rabbitChannel = await _rabbitConnection.CreateChannelAsync();
        
        // Exchange & Queue Declaration
        // Fanout Exchange: It means "broadcast to everyone" type of exchange.
        // When an alarm occurs, a copy is sent to every queue connected to this exchange (Dashboard, SMS, Mail, etc.).
        await _rabbitChannel.ExchangeDeclareAsync(_config["RabbitMQ:ExchangeName"]!, ExchangeType.Fanout);
        await _rabbitChannel.QueueDeclareAsync(_config["RabbitMQ:QueueName"]!, durable: true, exclusive: false, autoDelete: false);
        await _rabbitChannel.QueueBindAsync(_config["RabbitMQ:QueueName"]!, _config["RabbitMQ:ExchangeName"]!, routingKey: "");
    }

    private async Task PublishAlertAsync(AlertMessage alert)
    {
        // Why ReadOnlyMemory<byte>? Because it's a value type and it's more efficient than byte array.
        // ToByteArray(): It's used to convert Protobuf message to byte array which is understood by RabbitMQ.
        ReadOnlyMemory<byte> body = alert.ToByteArray();

        await _rabbitChannel!.BasicPublishAsync(
            exchange: _config["RabbitMQ:ExchangeName"]!,
            routingKey: "",
            body: body);

        _logger.LogWarning("ALERT SENT: Device {Id} - Temp {Val}", alert.DeviceId, alert.CurrentValue);
    }
}
