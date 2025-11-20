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
    }
}