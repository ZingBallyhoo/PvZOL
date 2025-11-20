using ArcticFox.Codec;
using ArcticFox.Codec.Binary;
using ArcticFox.Net;
using ArcticFox.Net.Sockets;
using ProtoBuf;
using PvZOL.Protocol.Cmd.Types;
using PvZOL.Protocol.TConnD;

namespace PvZOL.GameServer
{
    public class PvzSocket : HighLevelSocket, ISpanConsumer<byte>
    {
        public PvzSocket(SocketInterface socket) : base(socket)
        {
            m_netInputCodec = new CodecChain()
                .AddCodec(new PvzBufferCodec())
                .AddCodec(this);
        }

        public void Input(ReadOnlySpan<byte> input, ref object? state)
        {
            var pkgReader = new BitReader(input);
            var pkg = new TWebPvzPkg();
            pkg.Read(ref pkgReader);

            if (pkgReader.m_dataOffset != pkgReader.m_dataLength)
            {
                throw new InvalidDataException("pkg had trailing data");
            }
            
            var decryptedBody = pkg.Body.ToArray();
            for (var i = 0; i < decryptedBody.Length; i++)
            {
                decryptedBody[i] ^= 7;
            }
            
            var common = Serializer.Deserialize<CmdCommon>(decryptedBody);
            Console.Out.WriteLine(common);
            
            // todo: map cmd name -> type, deserialize, dispatch
        }

        public void Abort()
        {
            Close();
        }
    }
}