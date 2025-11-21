using ArcticFox.Codec.Binary;

namespace PvZOL.Protocol.TConnD
{
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

        public void Write(ref GrowingBitWriter writer)
        {
            var pkgStart = writer.m_dataOffset;
            
            Head.Write(ref writer);
            Ext.Write(ref writer, Head.Cmd);
            writer.WriteBytes(Body);

            // meh
            var pkgEnd = writer.m_dataOffset;
            Head.PkgLen = checked((uint)(writer.m_dataOffset - pkgStart));
            Head.BodyLen = checked((uint)Body.Length);
            writer.SeekByte((uint)pkgStart);
            Head.Write(ref writer);
            writer.SeekByte((uint)pkgEnd);
        }
    }
}