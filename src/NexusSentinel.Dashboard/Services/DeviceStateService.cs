using System.Text.Json;
using StackExchange.Redis;
using NexusSentinel.Shared.Protos;

namespace NexusSentinel.Dashboard.Services;

public class DeviceStateService(IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly IServer _server = redis.GetServer(redis.GetEndPoints().First());

    public async Task<List<TelemetryRecord>> GetLatestStatesAsync()
    {
        var devices = new List<TelemetryRecord>();

        try 
        {
            // SCAN for all keys starting with device:
            var keys = _server.Keys(pattern: "device:*").ToArray();

            foreach (var key in keys)
            {
                var json = await _db.StringGetAsync(key);
                if (!json.IsNullOrEmpty)
                {
                    var record = JsonSerializer.Deserialize<TelemetryRecord>(json!);
                    if (record != null)
                    {
                        devices.Add(record);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Redis Access Error: {ex}");
            throw; 
        }

        return devices;
    }
}