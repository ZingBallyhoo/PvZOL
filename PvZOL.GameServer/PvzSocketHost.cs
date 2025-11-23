using System.Net;
using ArcticFox.Net;
using ArcticFox.Net.Sockets;
using Microsoft.Extensions.Hosting;

namespace PvZOL.GameServer
{
    public class PvzSocketHost : SocketHost, IHostedService
    {
        private readonly TcpServer m_tcpServer;
        
        public PvzSocketHost()
        {
            m_tcpServer = new TcpServer(this, new IPEndPoint(IPAddress.Loopback, 8001));
        }
        
        public override HighLevelSocket CreateHighLevelSocket(SocketInterface socket)
        {
            return new PvzSocket(socket);
        }
        
        public override async Task StartAsync(CancellationToken cancellationToken=default)
        {
            await base.StartAsync(cancellationToken);
            m_tcpServer.StartAcceptWorker();
        }

        public override async Task StopAsync(CancellationToken cancellationToken=default)
        {
            await base.StopAsync(cancellationToken);
            m_tcpServer.Dispose();
        }
    }
}