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

        public TWebBase()
        {
            Magic = 0xCA;
            Version = 1;
        }
        
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
                case 0:
                {
                    // todo: is this allowed? it doesn't validate
                    break;
                }
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

        public void Write(ref GrowingBitWriter writer)
        {
            writer.WriteByte(Magic);
            writer.WriteUInt32BigEndian(PkgLen);
            writer.WriteUInt32BigEndian(BodyLen);
            writer.WriteByte(Version);
            writer.WriteByte(Cmd);
            writer.WriteByte(UserType);
            
            switch (UserType)
            {
                case 0:
                {
                    // todo: is this allowed? it doesn't validate
                    break;
                }
                case 1:
                {
                    QQUserSimple.Write(ref writer);
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