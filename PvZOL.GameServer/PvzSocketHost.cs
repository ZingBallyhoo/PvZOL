using ArcticFox.Net;
using ArcticFox.Net.Sockets;
using Microsoft.Extensions.Hosting;

namespace PvZOL.GameServer
{
    public class PvzSocketHost : SocketHost, IHostedService
    {
        public override HighLevelSocket CreateHighLevelSocket(SocketInterface socket)
        {
            return new PvzSocket(socket);
        }
    }
}