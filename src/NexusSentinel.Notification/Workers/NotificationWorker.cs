using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Google.Protobuf;
using NexusSentinel.Shared.Protos;
using Microsoft.AspNetCore.SignalR;
using NexusSentinel.Notification.Hubs;

namespace NexusSentinel.Notification.Workers;

/// <summary>
/// The NotificationWorker acts as a real-time bridge between the backend event bus (RabbitMQ) 
/// and the frontend notification layer (SignalR).
/// It consumes critical alerts and broadcasts them to all connected dashboard instances.
/// </summary>
public class NotificationWorker(
    ILogger<NotificationWorker> logger,
    IConfiguration config,
    IHubContext<AlertHub> hubContext) : BackgroundService
{
    /// <summary>
    /// Initializes the reliable messaging connection and begins listening for alert events.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Notification Gateway initialized. Listening for system alerts via RabbitMQ.");

        var factory = new ConnectionFactory { HostName = config["RabbitMQ:HostName"] ?? "localhost" };

        // Resource management via 'using' ensures connections are closed on service stop
        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        string exchange = config["RabbitMQ:ExchangeName"] ?? "alerts-exchange";
        string queue = config["RabbitMQ:QueueName"] ?? "alerts-queue";

        // Ensured infrastructure exists (Idempotent call)
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout);
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(queue, exchange, "");

        var consumer = new AsyncEventingBasicConsumer(channel);

        /* 
         * EVENT HANDLER: Process incoming alert from RabbitMQ.
         * The message is received as binary Protobuf, parsed, and then 
         * projected to an anonymous JSON-compatible object for SignalR delivery.
         */
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var alert = AlertMessage.Parser.ParseFrom(ea.Body.ToArray());

                logger.LogWarning("[GATEWAY] Alert dispatched to connected clients: Device {Id} ({Type})",
                    alert.DeviceId, alert.AlertType);

                // Push to all SignalR clients subscribing to "ReceiveAlert"
                await hubContext.Clients.All.SendAsync("ReceiveAlert", new
                {
                    alert.DeviceId,
                    alert.AlertType,
                    alert.CurrentValue,
                    alert.ThresholdValue,
                    alert.Severity,
                    alert.Timestamp
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch alert to SignalR.");
            }
        };

        // Start the continuous consumption process
        await channel.BasicConsumeAsync(queue: queue, autoAck: true, consumer: consumer);

        // Wait indefinitely until the background service is stopped
        await Task.Delay(Timeout.Infinite, stoppingToken);

        logger.LogWarning("Notification Gateway is shutting down.");
    }
}
