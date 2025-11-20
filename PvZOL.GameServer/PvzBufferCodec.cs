using System.Buffers.Binary;
using ArcticFox.Codec;

namespace PvZOL.GameServer
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
}