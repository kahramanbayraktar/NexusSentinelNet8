using System.Security;
using NexusSentinel.Persistence;
using Elastic.Clients.Elasticsearch;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton<ElasticsearchClient>(serviceProvider =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var settings = new ElasticsearchClientSettings(new Uri(config["Elasticsearch:Url"]!));
    return new ElasticsearchClient(settings);
});

var host = builder.Build();
host.Run();
