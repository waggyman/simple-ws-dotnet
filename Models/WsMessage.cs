using System.Text.Json.Serialization;

namespace SimpleWs.Models;

public sealed record WsMessage(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("data")] string? Data = null,
    [property: JsonPropertyName("timestamp")] DateTimeOffset? Timestamp = null);

public sealed record HealthResponse(
    string Status,
    int ActiveConnections,
    string WebSocketPath);
