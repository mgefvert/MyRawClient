using System;
using System.IO;
using System.Text;
using MyRawClient.Packets;

namespace MyRawClient.PacketHandler
{
    internal class DefaultPacketHandler : IPacketHandler
    {
        private readonly Encoding _encoding;
        private readonly byte[] _lastHeader = new byte[4];
        private readonly Stream _stream;
        private byte _sequence;

        public DefaultPacketHandler(Stream stream, Encoding encoding)
        {
            _stream = stream;
            _encoding = encoding;
        }

        public byte[] ReadPacket()
        {
            if (_stream.Read(_lastHeader, 0, 4) != 4)
                throw new MyRawException("Invalid data received from server, closing connection.");

            var sequence = _lastHeader[3];
            if (sequence != _sequence++)
                throw new MyRawException("Sequence ID out of sync, closing connection.");

            _lastHeader[3] = 0;
            var pktlen = BitConverter.ToInt32(_lastHeader, 0);
            _lastHeader[3] = sequence;

            var buffer = new byte[pktlen];
            if (_stream.Read(buffer, 0, pktlen) != pktlen)
                throw new MyRawException("Invalid data received from server, closing connection.");

            if (PacketReader.IsErrPacket(buffer))
                throw MyRawException.Decode(buffer, _encoding);

            return buffer;
        }

        public void SendPacket(byte[] buffer, bool newCommand)
        {
            var header = BitConverter.GetBytes((uint)buffer.Length);
            if (newCommand)
                _sequence = 0;

            header[3] = _sequence++;
            _stream.Write(header, 0, header.Length);
            _stream.Write(buffer, 0, buffer.Length);
        }
    }
}
