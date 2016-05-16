using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;

namespace Nyan.Modules.Web.Tools.WebSocket
{
    [RoutePrefix("stack/tools/websocket")]
    public class WebSocketMessagingController : ApiController
    {
        public HttpResponseMessage Get()
        {
            var currentContext = HttpContext.Current;
            if (currentContext.IsWebSocketRequest ||
                currentContext.IsWebSocketRequestUpgrading)
            {
                currentContext.AcceptWebSocketRequest(ProcessWebsocketSession);
            }

            return Request.CreateResponse(HttpStatusCode.SwitchingProtocols);
        }

        private Task ProcessWebsocketSession(AspNetWebSocketContext context)
        {
            var handler = new NyanWebSocketHandler();
            var processTask = handler.ProcessWebSocketRequestAsync(context);
            return processTask;
        }
    }
}
