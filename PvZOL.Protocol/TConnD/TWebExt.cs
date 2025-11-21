using ArcticFox.Codec.Binary;

namespace PvZOL.Protocol.TConnD
{
    public struct TWebExt
    {
        public void Read(ref BitReader reader, byte cmd)
        {
            if (cmd == 1)
            {
                throw new NotImplementedException("read ext because cmd is 1");
            }
        }
        
        public void Write(ref GrowingBitWriter writer, byte cmd)
        {
            if (cmd == 1)
            {
                throw new NotImplementedException("read ext because cmd is 1");
            }
        }
    }
}