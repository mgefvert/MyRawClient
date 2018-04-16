using System;
using System.Text;
using MyRawClient.Packets;

namespace MyRawClient
{
    public class MyRawException : Exception
    {
        public ushort ErrCode { get; set; }
        public string ErrText { get; set; }
        public string State { get; set; }
        public string StateMarker { get; set; }

        public MyRawException(string message) : base(message)
        {
        }

        public static MyRawException Decode(byte[] buffer, Encoding encoding)
        {
            var position = 1;
            var errCode = PacketReader.ReadInt2(buffer, ref position);
            var stateMarker = PacketReader.ReadStringFixed(buffer, ref position, 1, encoding);
            var state = PacketReader.ReadStringFixed(buffer, ref position, 5, encoding);
            var message = PacketReader.ReadStringToEnd(buffer, ref position, encoding);

            return new MyRawException($"ERR {errCode}: {stateMarker}{state} {message}")
            {
                ErrCode = errCode,
                State = state,
                StateMarker = stateMarker,
                ErrText = message
            };
        }
    }
}
