using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Google.Protobuf;
using NexusSentinel.Shared.Protos;
using Microsoft.AspNetCore.SignalR;
using NexusSentinel.Notification.Hubs;
using System.Threading;

public class NotificationWorker(ILogger<NotificationWorker> logger, IConfiguration config, IHubContext<AlertHub> hubContext) : BackgroundService
{
    // TODO: I need to understand this code better.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NotificationWorker started.");

        var factory = new ConnectionFactory { HostName = config["RabbitMQ:HostName"]! };
        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(config["RabbitMQ:ExchangeName"]!, ExchangeType.Fanout);
        await channel.QueueDeclareAsync(config["RabbitMQ:QueueName"]!, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(config["RabbitMQ:QueueName"]!, config["RabbitMQ:ExchangeName"]!, "");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var alert = AlertMessage.Parser.ParseFrom(ea.Body.ToArray());
            logger.LogInformation("Alert received for Device {Id}, pushing to SignalR!", alert.DeviceId);

            await hubContext.Clients.All.SendAsync("ReceiveAlert", 
            new {
                deviceId = alert.DeviceId,
                alertType = alert.AlertType,
                currentValue = alert.CurrentValue,
                thresholdValue = alert.ThresholdValue,
                severity = alert.Severity,
                timestamp = alert.Timestamp
            }, stoppingToken);

        };

        await channel.BasicConsumeAsync(queue: config["RabbitMQ:QueueName"]!, autoAck: true, consumer: consumer);
        await Task.Delay(-1, stoppingToken);

        logger.LogInformation("NotificationWorker stopped.");
    }
}