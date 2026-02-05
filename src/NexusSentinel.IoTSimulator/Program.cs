using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using NexusSentinel.Shared.Protos;

using var channel = GrpcChannel.ForAddress("http://localhost:5251");

var client = new TelemetryService.TelemetryServiceClient(channel);

Console.WriteLine("NexusSentinel IoT Simulator Starting...");
Console.WriteLine("Press any key to start sending telemetry...");
Console.ReadKey();

// This will create a stream of records to send to the server
var streamCalls = client.StreamTelemetry();
// This will get the writer for the stream
var streamWriter = streamCalls.RequestStream;

var random = new Random();

try
{
    while (true)
    {
        var record = new TelemetryRecord
        {
            DeviceId = "Simulated-Device-001",
            Temperature = 20 + (random.NextDouble() * 10), // Random temperature between 20 and 30
            Humidity = 40 + (random.NextDouble() * 20), // Random humidity between 40 and 60
            VibrationLevel = random.NextDouble() * 2, // Random vibration level between 0 and 2
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()          
        };

        // Send the record to the server
        await streamWriter.WriteAsync(record);

        Console.WriteLine($"[Sent] Temp: {record.Temperature:F2}°C | Vib: {record.VibrationLevel:F2}");

        // Wait for 1 second before sending the next record
        await Task.Delay(1000);        
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
finally
{
    // This will signal to the server that we are done sending records
    await streamWriter.CompleteAsync();
    // This will wait for the server to send a response
    var response = await streamCalls;

    Console.WriteLine($"[Response] {response.Message}");
}