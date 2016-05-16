using Microsoft.Web.WebSockets;

// http://stackoverflow.com/questions/25668398/using-websockets-with-asp-net-web-api

namespace Nyan.Modules.Web.Tools.WebSocket
{
    public class NyanWebSocketHandler : WebSocketHandler
    {
        private static readonly WebSocketCollection Clients = new WebSocketCollection();

        public override void OnOpen()
        {
            Clients.Add(this);
        }

        public override void OnMessage(string message)
        {
            Send("Echo: " + message);
        }
    }
}