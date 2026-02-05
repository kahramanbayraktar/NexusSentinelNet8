using NexusSentinel.Ingestion.Services;
using Confluent.Kafka;
using System; // Added for Kafka types

var builder = WebApplication.CreateBuilder(args);

// --- KAFKA PRODUCER SETUP START ---
// 1. Reading Kafka Configuration from appsettings.json.
// If "Kafka:BootstrapServers" is not configured, throw an exception (Fail Fast).
ProducerConfig producerConfig = new()
{
    BootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
        ?? throw new InvalidOperationException("Kafka:BootstrapServers is not configured")
};

// 2. Creating the Kafka Producer and adding it as a Singleton service.
// <string, string> -> Key and Value types are set to string (Currently sending JSON).
builder.Services.AddSingleton<IProducer<string, string>>(Span =>
{
    return new ProducerBuilder<string, string>(producerConfig).Build();
});
// --- KAFKA PRODUCER SETUP END ---

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
