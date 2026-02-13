using NexusSentinel.Ingestion.Services;
using Confluent.Kafka;
using System; // Added for Kafka types

var builder = WebApplication.CreateBuilder(args);

// --- KAFKA INFRASTRUCTURE CONFIGURATION ---

/* 
 * 1. Initialize Kafka Producer Configuration
 * BootstrapServers: The initial contact point for the Kafka cluster.
 * We use the 'Fail Fast' principle here: if Kafka isn't configured, the application won't start.
 */
var producerConfig = new ProducerConfig
{
    BootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
        ?? throw new InvalidOperationException("CRITICAL: Kafka:BootstrapServers is missing from configuration.")
};

/* 
 * 2. Register IProducer as a Singleton
 * Rationale: Kafka Producers are thread-safe and expensive to create. 
 * A single shared instance optimizes resource usage and improves performance.
 * We use <string, byte[]> to send binary Protobuf payloads for maximum efficiency.
 */
builder.Services.AddSingleton<IProducer<string, byte[]>>(sp =>
{
    return new ProducerBuilder<string, byte[]>(producerConfig).Build();
});

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
// This line registers the TelemetryIngestionService with the gRPC server.
// It tells the server that when a request comes in for the TelemetryIngestionService, 
// it should use the TelemetryIngestionService class to handle the request.
// The default endpoint for gRPC services is /<PackageName>.<ServiceName>/
// For example, if the package name is com.example.telemetry and the service name is TelemetryService, 
// the default endpoint will be /com.example.telemetry.TelemetryService/
// In this case, the package name is NexusSentinel.Ingestion and the service name is TelemetryIngestionService, 
// so the default endpoint will be /NexusSentinel.Ingestion.TelemetryIngestionService/
app.MapGrpcService<TelemetryIngestionService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
