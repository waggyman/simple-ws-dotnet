using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using SimpleWs.Models;
using SimpleWs.Services;

namespace SimpleWs.WebSocket;

public sealed class WebSocketHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ConnectionManager _connections;
    private readonly IMessageHandler _messageHandler;
    private readonly ILogger<WebSocketHandler> _logger;

    public WebSocketHandler(
        ConnectionManager connections,
        IMessageHandler messageHandler,
        ILogger<WebSocketHandler> logger)
    {
        _connections = connections;
        _messageHandler = messageHandler;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Expected a WebSocket request.");
            return;
        }

        var socket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString("N")[..8];
        _connections.Add(connectionId, socket);

        _logger.LogInformation("Client {ConnectionId} connected", connectionId);

        try
        {
            await ListenAsync(socket, connectionId);
        }
        finally
        {
            _connections.Remove(connectionId);
            _logger.LogInformation("Client {ConnectionId} disconnected", connectionId);

            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }

            socket.Dispose();
        }
    }

    private async Task ListenAsync(System.Net.WebSockets.WebSocket socket, string connectionId)
    {
        var buffer = new byte[4096];

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (result.MessageType != WebSocketMessageType.Text)
            {
                await SendAsync(socket, new WsMessage("error", "Only text messages are supported", DateTimeOffset.UtcNow));
                continue;
            }

            var raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
            await HandleMessageAsync(socket, raw, connectionId);
        }
    }

    private async Task HandleMessageAsync(System.Net.WebSockets.WebSocket socket, string raw, string connectionId)
    {
        WsMessage incoming;

        try
        {
            incoming = JsonSerializer.Deserialize<WsMessage>(raw, JsonOptions)
                ?? throw new JsonException();

            if (string.IsNullOrWhiteSpace(incoming.Action))
            {
                await SendAsync(socket, new WsMessage("error", "Message must include an action field", DateTimeOffset.UtcNow));
                return;
            }
        }
        catch (JsonException)
        {
            await SendAsync(socket, new WsMessage("error", "Invalid JSON", DateTimeOffset.UtcNow));
            return;
        }

        _logger.LogDebug("Client {ConnectionId} sent {Action}", connectionId, incoming.Action);

        if (incoming.Action.Trim().Equals("broadcast", StringComparison.OrdinalIgnoreCase))
        {
            var message = new WsMessage("broadcast", incoming.Data, DateTimeOffset.UtcNow);
            await BroadcastAsync(message);
            return;
        }

        var response = _messageHandler.Handle(incoming);
        await SendAsync(socket, response);
    }

    private async Task BroadcastAsync(WsMessage message)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        foreach (var connection in _connections.GetOpenConnections())
        {
            await connection.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
        }
    }

    private static async Task SendAsync(System.Net.WebSockets.WebSocket socket, WsMessage message)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
    }
}
