using System.Net.WebSockets;
using System.Text;

namespace simple_ws_dotnet.Tests;

public class WebSocketEndpointTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly Uri WsUri = new("ws://localhost/ws");

    [Fact]
    public async Task Ping_ReturnsPong()
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(WsUri, CancellationToken.None);

        await SendAsync(socket, """{"action":"ping"}""");
        var body = await ReceiveAsync(socket);

        Assert.Contains("pong", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Echo_ReturnsSameData()
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(WsUri, CancellationToken.None);

        await SendAsync(socket, """{"action":"echo","data":"hello"}""");
        var body = await ReceiveAsync(socket);

        Assert.Contains("echo", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hello", body);
    }

    [Fact]
    public async Task InvalidJson_ReturnsError()
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(WsUri, CancellationToken.None);

        await SendAsync(socket, "not-json");
        var body = await ReceiveAsync(socket);

        Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Invalid JSON", body);
    }

    [Fact]
    public async Task UnknownAction_ReturnsError()
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket = await wsClient.ConnectAsync(WsUri, CancellationToken.None);

        await SendAsync(socket, """{"action":"subscribe"}""");
        var body = await ReceiveAsync(socket);

        Assert.Contains("error", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unknown action", body);
    }

    [Fact]
    public async Task Broadcast_SendsToAllConnectedClients()
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        using var socket1 = await wsClient.ConnectAsync(WsUri, CancellationToken.None);
        using var socket2 = await wsClient.ConnectAsync(WsUri, CancellationToken.None);

        await SendAsync(socket1, """{"action":"broadcast","data":"hello all"}""");

        var msg1 = await ReceiveAsync(socket1);
        var msg2 = await ReceiveAsync(socket2);

        Assert.Contains("broadcast", msg1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hello all", msg1);
        Assert.Contains("broadcast", msg2, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hello all", msg2);
    }

    private static async Task SendAsync(WebSocket socket, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
    }

    private static async Task<string> ReceiveAsync(WebSocket socket)
    {
        var buffer = new byte[4096];
        var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }
}
