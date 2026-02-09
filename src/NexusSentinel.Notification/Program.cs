using System.Runtime.InteropServices;
using NexusSentinel.Notification.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddHostedService<NotificationWorker>();

builder.Services.AddCors(options =>{
    options.AddDefaultPolicy(builder =>
    {
        builder.SetIsOriginAllowed(_ => true) // allow all origins for development only
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => "Hello World!");
// Here <AlertHub> is the name of the hub class.
// And "/alertHub" is the name of the endpoint.
app.MapHub<AlertHub>("/alertHub");

app.Run();
