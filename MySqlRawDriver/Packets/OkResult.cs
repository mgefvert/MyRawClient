using System;
using System.Text;
using MySqlRawDriver.Enumerations;

namespace MySqlRawDriver.Packets
{
    public class OkResult
    {
        public ulong AffectedRows { get; set; }
        public ulong LastInsertId { get; set; }
        public StatusFlags Status { get; set; }
        public ushort Warnings { get; set; }
        public string Info { get; set; }

        public override string ToString()
        {
            return $"OK/EOF rows={AffectedRows} lastId={LastInsertId} status={Status} warn={Warnings} info={Info}";
        }

        public static OkResult Decode(byte[] buffer, Encoding encoding)
        {
            var position = 0;
            var mode = (ResultMode) PacketReader.ReadInt1(buffer, ref position);
            switch (mode)
            {
                case ResultMode.Ok:
                    return new OkResult
                    {
                        AffectedRows = PacketReader.ReadIntLengthEncoded(buffer, ref position),
                        LastInsertId = PacketReader.ReadIntLengthEncoded(buffer, ref position),
                        Status = (StatusFlags) PacketReader.ReadInt2(buffer, ref position),
                        Warnings = PacketReader.ReadInt2(buffer, ref position),
                        Info = PacketReader.ReadStringToEnd(buffer, ref position, encoding)
                    };

                case ResultMode.Eof:
                    return new OkResult
                    {
                        Warnings = PacketReader.ReadInt2(buffer, ref position),
                        Status = (StatusFlags) PacketReader.ReadInt2(buffer, ref position)
                    };

                default:
                    throw new MyRawException("Expected EOF (FE) or OK (00) packet, but got " + mode.ToString("X2"));
            }
        }
    }
}
