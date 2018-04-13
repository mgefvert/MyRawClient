using System;
using System.Text;
using MySqlRawDriver.Enumerations;

namespace MySqlRawDriver.Packets
{
    public class Result
    {
        public long RowsAffected { get; set; }
        public string Info { get; set; }
        public long LastInsertId { get; set; }
        public StatusFlags Status { get; set; }
        public int Warnings { get; set; }

        public override string ToString()
        {
            return $"OK/EOF rows={RowsAffected} lastId={LastInsertId} status={Status} warn={Warnings} info={Info}";
        }

        public static Result Decode(byte[] buffer, Encoding encoding)
        {
            var position = 0;
            var mode = (ResultMode) PacketReader.ReadInt1(buffer, ref position);
            switch (mode)
            {
                case ResultMode.Ok:
                    return new Result
                    {
                        RowsAffected = (long)PacketReader.ReadIntLengthEncoded(buffer, ref position),
                        LastInsertId = (long)PacketReader.ReadIntLengthEncoded(buffer, ref position),
                        Status = (StatusFlags) PacketReader.ReadInt2(buffer, ref position),
                        Warnings = PacketReader.ReadInt2(buffer, ref position),
                        Info = PacketReader.ReadStringToEnd(buffer, ref position, encoding)
                    };

                case ResultMode.Eof:
                    return new Result
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
