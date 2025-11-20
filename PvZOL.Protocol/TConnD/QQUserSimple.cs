using System.Text;
using ArcticFox.Codec.Binary;

namespace PvZOL.Protocol.TConnD
{
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
}