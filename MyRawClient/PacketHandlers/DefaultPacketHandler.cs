using System;
using System.IO;
using System.Text;
using MyRawClient.Packets;

namespace MyRawClient.PacketHandlers
{
    internal class DefaultPacketHandler : IPacketHandler
    {
        private readonly Encoding _encoding;
        private readonly byte[] _lastHeader = new byte[4];
        private byte _sequence;

        public DefaultPacketHandler(Encoding encoding)
        {
            _encoding = encoding;
        }

        public byte[] ReadPacket(Stream stream)
        {
            if (stream.Read(_lastHeader, 0, 4) != 4)
                throw new MyRawException("Invalid data received from server, closing connection.");

            var sequence = _lastHeader[3];
            _sequence = (byte)(sequence + 1);

            _lastHeader[3] = 0;
            var pktlen = BitConverter.ToInt32(_lastHeader, 0);

            var buffer = new byte[pktlen];
            if (stream.Read(buffer, 0, pktlen) != pktlen)
                throw new MyRawException("Invalid data received from server, closing connection.");

            if (PacketReader.IsErrPacket(buffer))
                throw MyRawException.Decode(buffer, _encoding);

            return buffer;
        }

        public void SendPacket(Stream stream, byte[] buffer, bool newCommand)
        {
            var header = BitConverter.GetBytes((uint)buffer.Length);
            if (newCommand)
                _sequence = 0;

            header[3] = _sequence++;
            stream.Write(header, 0, header.Length);
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}
