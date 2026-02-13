using Confluent.Kafka;
using Grpc.Core;
using NexusSentinel.Shared.Protos;
using Google.Protobuf;

namespace NexusSentinel.Ingestion.Services;

/// <summary>
/// High-performance gRPC service responsible for accepting telemetry streams from IoT devices.
/// Acting as a decoupled entry point, it validates and forwards incoming data to the Kafka event backbone.
/// </summary>
public class TelemetryIngestionService : TelemetryService.TelemetryServiceBase
{
    private readonly ILogger<TelemetryIngestionService> _logger;
    private readonly IProducer<string, byte[]> _producer; // Refactored to use binary payload for performance

    public TelemetryIngestionService(ILogger<TelemetryIngestionService> logger, IProducer<string, byte[]> producer)
    {
        _logger = logger;
        _producer = producer;
    }

    /// <summary>
    /// Processes a bidirectional or client-side stream of telemetry records.
    /// Messages are ingested and immediately offloaded to Kafka to maintain a non-blocking entry point.
    /// </summary>
    /// <param name="requestStream">The stream of incoming telemetry records from the client.</param>
    /// <param name="context">The server-side context for the gRPC call.</param>
    /// <returns>A summary acknowledgment once the stream is closed by the client.</returns>
    public override async Task<TelemetryAck> StreamTelemetry(IAsyncStreamReader<TelemetryRecord> requestStream, ServerCallContext context)
    {
        int recordCount = 0;
        
        // Iterating through the gRPC stream using ReadAllAsync for efficient async consumption
        await foreach(var record in requestStream.ReadAllAsync())
        {
            recordCount++;
            
            // Log incoming data for observability (consider reducing frequency in high-traffic production)
            _logger.LogInformation("Ingesting Data -> Device: {DeviceId} | Temp: {Temp:F2}°C", 
                record.DeviceId, record.Temperature);

            /* 
             * STRATEGY: Switch to Binary Protobuf over Kafka.
             * Rationale: Binary serialization is significantly faster and results in 
             * ~40-60% smaller payloads compared to JSON, saving network bandwidth and disk space.
             */
            byte[] binaryPayload = record.ToByteArray();

            // Produce to Kafka using the device ID as the message key to ensure partition affinity (ordering)
            _producer.Produce("telemetry", new Message<string, byte[]>
            {
                Key = record.DeviceId,
                Value = binaryPayload
            });
        }

        _logger.LogInformation("Cleanly closed telemetry stream from device. Total records: {Count}", recordCount);

        return new TelemetryAck 
        { 
            Success = true, 
            Message = $"NexusSentinel accepted {recordCount} telemetry packets." 
        };
    }
}
