using SimpleWs.Models;

namespace SimpleWs.Services;

public interface IMessageHandler
{
    WsMessage Handle(WsMessage incoming);
}
