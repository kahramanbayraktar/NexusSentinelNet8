using System.Runtime.InteropServices;
using NexusSentinel.Notification.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddSignalR(e =>
{
    e.EnableDetailedErrors = true;
    e.MaximumReceiveMessageSize = 102400000;
});
builder.Services.AddHostedService<NotificationWorker>();

builder.Services.AddCors(options =>{
    options.AddDefaultPolicy(builder =>
    {
        builder.SetIsOriginAllowed(_ => true) // allow all origins for development only
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => "Hello World!");
// Here <AlertHub> is the name of the hub class.
// And "/alertHub" is the name of the endpoint.
app.MapHub<AlertHub>("/alertHub");

app.Run();
