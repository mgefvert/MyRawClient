using System;
using System.Text;
using MyRawClient.Enumerations;

namespace MyRawClient.Packets
{
    public class HandshakeResponse
    {
        public CapabilityFlags Capabilities { get; set; }
        public byte CharacterSet { get; set; }
        public string Database { get; set; }
        public uint MaxPacketSize { get; set; }
        public byte[] Password { get; set; }
        public string User { get; set; }

        public byte[] Encode(Encoding encoding, byte sequence)
        {
            var builder = new PacketBuilder(encoding, sequence);

            builder.AppendInt4((uint)Capabilities);
            builder.AppendInt4(MaxPacketSize); // Max packet size
            builder.AppendInt1(CharacterSet); // UTF8
            builder.AppendBytes(new byte[23]);
            builder.AppendStringNullTerminated(User);
            builder.AppendBytes(Password);
            builder.AppendStringNullTerminated(Database);

            return builder.ToPacket();
        }
    }
}
