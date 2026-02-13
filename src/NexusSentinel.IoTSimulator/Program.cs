using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using NexusSentinel.Shared.Protos;

/*
 * NexusSentinel IoT Simulator
 * This utility simulates high-frequency industrial sensors pumping telemetry 
 * data into the NexusSentinel ecosystem via gRPC Client-to-Server streaming.
 */

// Service Discovery: Use 'IngestionServerUrl' environment variable (Docker) or fallback to Localhost
var serverUrl = Environment.GetEnvironmentVariable("IngestionServerUrl") ?? "http://localhost:5251";

_ = Console.Title = "NexusSentinel - IoT Simulator";
Console.WriteLine("--------------------------------------------------");
Console.WriteLine($"📡 Starting NexusSentinel IoT Simulator");
Console.WriteLine($"🛰️ Connecting to Ingestion: {serverUrl}");
Console.WriteLine("--------------------------------------------------");

// Initialize the high-performance gRPC channel
using var channel = GrpcChannel.ForAddress(serverUrl);
var client = new TelemetryService.TelemetryServiceClient(channel);

/* 
 * ESTABLISHING STREAM:
 * rpc StreamTelemetry (stream TelemetryRecord) returns (TelemetryAck);
 * This creates a persistent HTTP/2 stream where multiple records can be pushed 
 * without the overhead of repeated handshakes.
 */
var streamCall = client.StreamTelemetry();
var streamWriter = streamCall.RequestStream;

var random = new Random();
const string DeviceId = "DEV-PROTOTYPE-001";

try
{
    while (true)
    {
        // Generating synthetic telemetry data
        var record = new TelemetryRecord
        {
            DeviceId = DeviceId,
            Temperature = 20 + (random.NextDouble() * 30), // Range: 20-50°C
            Humidity = 30 + (random.NextDouble() * 40),    // Range: 30-70%
            VibrationLevel = random.NextDouble() * 5.0,    // Critical for anomaly testing
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        // Async write to the gRPC stream
        await streamWriter.WriteAsync(record);

        Console.ForegroundColor = record.Temperature > 45 ? ConsoleColor.Red : ConsoleColor.Green;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⬆️ Pushed -> Temp: {record.Temperature:F2}°C | Vib: {record.VibrationLevel:F2}");
        Console.ResetColor();

        // Simulate 1Hz data frequency
        await Task.Delay(1000);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Stream Interrupted: {ex.Message}");
}
finally
{
    /* 
     * GRACEFUL SHUTDOWN:
     * Tell the server we are finished writing, then wait for the final TelemetryAck.
     */
    await streamWriter.CompleteAsync();
    var response = await streamCall;

    Console.WriteLine($"\n✅ Server Acknowledged: {response.Message}");
}