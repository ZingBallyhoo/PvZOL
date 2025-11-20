using ArcticFox.Codec.Binary;

namespace PvZOL.Protocol.TConnD
{
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
}