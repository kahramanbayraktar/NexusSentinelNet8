using System.Threading;
using NexusSentinel.Processor;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// REDIS CONNECTION SETUP
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ??
    throw new InvalidOperationException("Redis:ConnectionString not found in configuration.");

// ConnectionMultiplexer is an expensive object to create, so we use a singleton.
// Throughout the application, it manages a single TCP connection to the Redis server.
// It handles connection pooling, automatic reconnection, and message routing.
// By "connection pooling", we mean that it manages multiple connections to the Redis server.
// By "automatic reconnection", we mean that it automatically reconnects to the Redis server if the connection is lost.
// By "message routing", we mean that it automatically routes messages to the appropriate Redis server.
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnectionString));
// REDIS CONNECTION SETUP END

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
