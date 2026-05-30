using SimpleWs.Models;
using SimpleWs.Services;
using SimpleWs.WebSocket;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConnectionManager>();
builder.Services.AddSingleton<IMessageHandler, MessageHandler>();
builder.Services.AddSingleton<WebSocketHandler>();

var app = builder.Build();

app.UseWebSockets();

var wsPath = builder.Configuration["WebSocket:Path"] ?? "/ws";

app.MapGet("/", () => Results.Ok(new
{
    name = "Simple WebSocket Server",
    websocket = wsPath,
    health = "/health"
}));

app.MapGet("/health", (ConnectionManager connections) =>
    Results.Ok(new HealthResponse("ok", connections.Count, wsPath)));

app.Map(wsPath, async (WebSocketHandler handler, HttpContext context) =>
    await handler.HandleAsync(context));

app.Run();

public partial class Program;
