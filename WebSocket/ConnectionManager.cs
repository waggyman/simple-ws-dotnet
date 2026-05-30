using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace SimpleWs.WebSocket;

public sealed class ConnectionManager
{
    private readonly ConcurrentDictionary<string, System.Net.WebSockets.WebSocket> _connections = new();

    public int Count => _connections.Count;

    public void Add(string id, System.Net.WebSockets.WebSocket socket) =>
        _connections[id] = socket;

    public void Remove(string id) =>
        _connections.TryRemove(id, out _);

    public IEnumerable<System.Net.WebSockets.WebSocket> GetOpenConnections() =>
        _connections.Values.Where(socket => socket.State == WebSocketState.Open);
}
