using ArcticFox.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using PvZOL.GameServer;

namespace PvZOL.Server.Controllers
{
    public class WebsocketController : Controller
    {
        private readonly PvzSocketHost m_socketHost;
        
        public WebsocketController(PvzSocketHost socketHost)
        {
            m_socketHost = socketHost;
        }
        
        [Route("/ws")]
        public async Task Get(CancellationToken cancellationToken)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            
            await AcceptWebsocket();
        }
        
        private async Task AcceptWebsocket()
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var socket = new WebSocketInterface(webSocket, true);
            
            var hl = m_socketHost.CreateHighLevelSocket(socket);
            await m_socketHost.AddSocket(hl);

            var tcs = new TaskCompletionSource();
            socket.m_cancellationTokenSource.Token.Register(() => tcs.SetResult());
            await tcs.Task;
        }
    }
}