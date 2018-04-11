using System;
using System.Text;
using MySqlRawDriver.Enumerations;

namespace MySqlRawDriver.Packets
{
    public class HandshakeRequest
    {
        public byte[] AuthData { get; set; }
        public CapabilityFlags Capabilities { get; set; }
        public byte CharacterSet { get; set; }
        public uint ConnectionId { get; set; }
        public string ServerVersion { get; set; }
        public StatusFlags Status { get; set; }
        public byte Version { get; set; }

        public static HandshakeRequest Decode(byte[] buffer, Encoding encoding)
        {
            var result = new HandshakeRequest();
            var position = 0;
            
            result.Version = PacketReader.ReadInt1(buffer, ref position);
            if (result.Version != 10)
                throw new MyRawException("Unable to handle protocol version " + result.Version);

            result.ServerVersion = PacketReader.ReadStringNullTerminated(buffer, ref position, encoding);
            result.ConnectionId = PacketReader.ReadInt4(buffer, ref position);
            var auth1 = PacketReader.ReadBytesFixed(buffer, ref position, 8);
            PacketReader.ReadInt1(buffer, ref position);
            uint caps = PacketReader.ReadInt2(buffer, ref position);

            byte[] auth2;
            if (!PacketReader.EndOfPacket(buffer, position))
            {
                result.CharacterSet = PacketReader.ReadInt1(buffer, ref position);
                result.Status = (StatusFlags)PacketReader.ReadInt2(buffer, ref position);
                caps |= (uint)PacketReader.ReadInt2(buffer, ref position) >> 16;

                var authlen = PacketReader.ReadInt1(buffer, ref position);
                PacketReader.ReadStringFixed(buffer, ref position, 10, encoding);
                auth2 = PacketReader.ReadBytesFixed(buffer, ref position, authlen - 8);
            }
            else
                auth2 = new byte[0];

            result.Capabilities = (CapabilityFlags)caps;
            result.AuthData = new byte[auth1.Length + auth2.Length - 1];
            Array.Copy(auth1, 0, result.AuthData, 0, auth1.Length);
            Array.Copy(auth2, 0, result.AuthData, auth1.Length, auth2.Length - 1);

            return result;
        }
    }
}
