using SimpleWs.Models;

namespace SimpleWs.Services;

public sealed class MessageHandler : IMessageHandler
{
    public WsMessage Handle(WsMessage incoming)
    {
        var action = incoming.Action.Trim().ToLowerInvariant();

        return action switch
        {
            "echo" => new WsMessage("echo", incoming.Data, DateTimeOffset.UtcNow),
            "ping" => new WsMessage("pong", null, DateTimeOffset.UtcNow),
            _ => new WsMessage("error", $"Unknown action: {incoming.Action}", DateTimeOffset.UtcNow)
        };
    }
}
