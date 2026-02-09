using Microsoft.AspNetCore.SignalR;

namespace NexusSentinel.Notification.Hubs;

public class AlertHub : Hub
{
    private readonly ILogger<AlertHub> _logger;

    public AlertHub(ILogger<AlertHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogWarning("CLIENT CONNECTED: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogWarning("CLIENT DISCONNECTED: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}