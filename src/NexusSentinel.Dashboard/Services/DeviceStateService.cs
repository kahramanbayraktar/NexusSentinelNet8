using System.Text.Json;
using StackExchange.Redis;
using NexusSentinel.Shared.Protos;

namespace NexusSentinel.Dashboard.Services;

public class DeviceStateService(IConnectionMultiplexer redis, ILogger<DeviceStateService> logger)
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<List<TelemetryRecord>> GetLatestStatesAsync()
    {
        var devices = new List<TelemetryRecord>();

        try 
        {
            // Get the server from the multiplexer. 
            // We do this here instead of constructor to ensure connection logic is handled safely.
            var endpoint = redis.GetEndPoints().FirstOrDefault();
            if (endpoint == null)
            {
                logger.LogWarning("Redis endpoint not found.");
                return devices;
            }

            var server = redis.GetServer(endpoint);

            // Use KeysAsync if available (it returns IAsyncEnumerable in newer versions)  
            // or wrap the synchronous Keys call if needed.
            // StackExchange.Redis Keys() returns IEnumerable which uses SCAN internally.
            // Executing it on the UI thread can block.
            
            // We use Task.Run to offload the synchronous blocking enumeration of Keys() 
            // to a thread pool thread, preventing UI freeze.
            await Task.Run(async () => 
            {
                // Note: Keys() is lazy. The network calls happen during iteration.
                foreach (var key in server.Keys(pattern: "device:*"))
                {
                    // StringGetAsync is async, but the loop itself is driven by synchronous enumerator.
                    var json = await _db.StringGetAsync(key);
                    if (!json.IsNullOrEmpty)
                    {
                        try 
                        {
                            var record = JsonSerializer.Deserialize<TelemetryRecord>(json!);
                            if (record != null)
                            {
                                lock(devices) 
                                {
                                    devices.Add(record);
                                }
                            }
                        }
                        catch (JsonException jex)
                        {
                            logger.LogError(jex, "Error deserializing telemetry record for key {Key}", key);
                        }
                    }
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching device states from Redis.");
            // We rethrow to let the UI know something went wrong
            throw; 
        }

        return devices;
    }
}