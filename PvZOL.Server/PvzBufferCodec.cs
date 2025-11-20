using System.Buffers.Binary;
using System.Text;
using ArcticFox.Codec;
using ArcticFox.Codec.Binary;
using ArcticFox.Net;
using ArcticFox.Net.Sockets;

namespace PvZOL.Server
{
    public class PvzBufferCodec : SpanCodec<byte, byte>
    {
        private readonly FixedSizeHeader<byte> m_header = new FixedSizeHeader<byte>(5);
        private readonly SizeBufferer<byte> m_body = new SizeBufferer<byte>();
        
        public override void Input(ReadOnlySpan<byte> input, ref object? state)
        {
            while (input.Length > 0)
            {
                if (m_header.ConsumeAndGet(ref input, out var header))
                {
                    var size = BinaryPrimitives.ReadInt32BigEndian(header.Slice(1));
                    m_body.SetSize(size); // todo: size limit
                    
                    // size is inclusive of header
                    var completeAlready = m_body.ConsumeAndGet(ref header, out _);
                    if (completeAlready) throw new InvalidDataException();
                }
            
                if (!m_body.ConsumeAndGet(ref input, out var body)) continue;
                CodecOutput(body, ref state);

                m_header.ResetOffset();
                m_body.ResetOffset();            
            }
        }
    }

    public struct QQUserSimple
    {
        public uint Uin;
        public string SKey;

        public void Read(ref BitReader reader)
        {
            Uin = reader.ReadUInt32BigEndian();

            var sKeyLen = reader.ReadInt32BigEndian();
            var sKeyBytes = reader.ReadBytes(sKeyLen - 1);
            var zeroTerm = reader.ReadByte();
            if (zeroTerm != 0) throw new InvalidDataException($"zero terminator invalid: {zeroTerm}");
            if (sKeyBytes.IndexOf((byte)0) != -1) throw new InvalidDataException("smuggled null character");
            
            SKey = Encoding.UTF8.GetString(sKeyBytes);
        }
    }

    public struct TWebBase
    {
        public byte Magic;
        public uint PkgLen;
        public uint BodyLen;
        public byte Version;
        public byte Cmd;
        public byte UserType;

        public QQUserSimple QQUserSimple;
        
        public void Read(ref BitReader reader)
        {
            Magic = reader.ReadByte();
            PkgLen = reader.ReadUInt32BigEndian();
            BodyLen = reader.ReadUInt32BigEndian();
            Version = reader.ReadByte();
            Cmd = reader.ReadByte();
            UserType = reader.ReadByte();

            switch (UserType)
            {
                case 1:
                {
                    QQUserSimple.Read(ref reader);
                    break;
                }
                default:
                {
                    throw new InvalidDataException($"unknown user type: {UserType}");
                }
            }
        }
    }

    public struct TWebExt
    {
        public void Read(ref BitReader reader, byte cmd)
        {
            if (cmd == 1)
            {
                throw new NotImplementedException("read ext because cmd is 1");
            }
        }
    }

    public ref struct TWebPvzPkg
    {
        public TWebBase Head;
        public TWebExt Ext;
        public ReadOnlySpan<byte> Body;
        
        public TWebPvzPkg()
        {
        }

        public void Read(ref BitReader reader)
        {
            Head.Read(ref reader);
            Ext.Read(ref reader, Head.Cmd);
            Body = reader.ReadBytes(checked((int)Head.BodyLen));
        }
    }

    public class PvzSocketHost : SocketHost, IHostedService
    {
        public override HighLevelSocket CreateHighLevelSocket(SocketInterface socket)
        {
            return new PvzSocket(socket);
        }
    }

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
            
            Console.Out.WriteLine($"data: {Convert.ToBase64String(decryptedBody)}");
        }

        public void Abort()
        {
            Close();
        }
    }
}