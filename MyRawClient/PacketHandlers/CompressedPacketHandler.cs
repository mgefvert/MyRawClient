using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using MyRawClient.Internal;
using MyRawClient.Packets;

namespace MyRawClient.PacketHandlers
{
    internal class CompressedPacketHandler : IPacketHandler
    {
        private readonly IPacketHandler _handler;
        private readonly byte[] _lastHeader = new byte[7];
        private readonly MemoryStream _memorybuffer = new MemoryStream();
        private byte _sequence;

        public CompressedPacketHandler(IPacketHandler handler)
        {
            _handler = handler;
        }

        // Since DeflateStream doesn't add the zlib init header and post-data checksum, we have to do
        // some trickery.
        private byte[] Compress(byte[] buffer)
        {
            using (var compressed = new MemoryStream())
            {
                // Header
                compressed.WriteByte(0x78);
                compressed.WriteByte(0x9c);

                using (var deflate = new DeflateStream(compressed, CompressionMode.Compress, true))
                {
                    deflate.Write(buffer, 0, buffer.Length);
                }

                // Adler-32 checksum
                var checksum = Checksum.Adler32(buffer);
                foreach(var b in BitConverter.GetBytes(checksum).Reverse())
                    compressed.WriteByte(b);

                return compressed.ToArray();
            }
        }

        private byte[] Decompress(byte[] buffer, int decompressedLength)
        {
            if (decompressedLength == 0)
                return buffer;  // Uncompressed data

            // Skip header and checksum
            using (var compressed = new MemoryStream(buffer, 2, buffer.Length - 6))
            {
                var result = new byte[decompressedLength];
                using (var deflate = new DeflateStream(compressed, CompressionMode.Decompress, true))
                {
                    deflate.Read(result, 0, decompressedLength);
                }

                // Adler-32 checksum
                var checksum = BitConverter.ToUInt32(BitConverter.GetBytes(Checksum.Adler32(result)).Reverse().ToArray(), 0);
                var original = BitConverter.ToUInt32(buffer, buffer.Length - 4);
                if (checksum != original)
                    throw new MyRawException("Checksum failure in compressed data stream.");

                return result;
            }
        }

        private bool PacketAvailable()
        {
            if (_memorybuffer.Length - _memorybuffer.Position < 4)
                return false;

            var position = _memorybuffer.Position;
            _memorybuffer.Read(_lastHeader, 0, 4);
            _lastHeader[3] = 0;
            var pktlen = BitConverter.ToUInt32(_lastHeader, 0);
            var remaining = _memorybuffer.Length - _memorybuffer.Position;
            _memorybuffer.Position = position;

            return remaining >= pktlen;
        }

        public byte[] ReadPacket(Stream stream)
        {
            while (!PacketAvailable())
            {
                // We can't extract a complete package yet. Fill the buffers.
                ReadAndDecompress(stream);
            }

            // Read one package
            var result = _handler.ReadPacket(_memorybuffer);

            // Is the buffer clear for now? If so, dispose all the contents and start over
            if (_memorybuffer.Position == _memorybuffer.Length)
            {
                _memorybuffer.Position = 0;
                _memorybuffer.SetLength(0);
            }

            return result;
        }

        private void ReadAndDecompress(Stream stream)
        {
            if (stream.Read(_lastHeader, 0, 7) != 7)
                throw new MyRawException("Invalid data received from server, closing connection.");

            var pos = 0;
            var compressedLen = PacketReader.ReadInt3(_lastHeader, ref pos);
            var sequence = PacketReader.ReadInt1(_lastHeader, ref pos);
            var uncompressedLen = PacketReader.ReadInt3(_lastHeader, ref pos);

            if (sequence != _sequence++)
                throw new MyRawException("Sequence ID out of sync, closing connection.");

            var source = new byte[compressedLen];
            stream.Read(source, 0, (int) compressedLen);

            var decompressed = Decompress(source, (int) uncompressedLen);
            
            var position = _memorybuffer.Position;
            _memorybuffer.Position = _memorybuffer.Length;
            _memorybuffer.Write(decompressed, 0, decompressed.Length);
            _memorybuffer.Position = position;
        }

        public void SendPacket(Stream stream, byte[] buffer, bool newCommand)
        {
            if (newCommand)
                _sequence = 0;

            if (buffer.Length < 50)
                SendUncompressedPacket(stream, buffer, newCommand);
            else
                SendCompressedPacket(stream, buffer, newCommand);
        }

        private void SendCompressedPacket(Stream stream, byte[] buffer, bool newCommand)
        {
            using (var mem = new MemoryStream())
            {
                // Let the previous packet handler finalize first.
                _handler.SendPacket(mem, buffer, newCommand);
                buffer = mem.ToArray();
            }

            var compressed = Compress(buffer);

            var header = new byte[7];
            WriteInt3(header, 0, compressed.Length);
            header[3] = _sequence++;
            WriteInt3(header, 4, buffer.Length);

            stream.Write(header, 0, 7);
            stream.Write(compressed, 0, compressed.Length);
        }

        private void SendUncompressedPacket(Stream stream, byte[] buffer, bool newCommand)
        {
            using (var mem = new MemoryStream())
            {
                _handler.SendPacket(mem, buffer, newCommand);

                var header = new byte[7];
                WriteInt3(header, 0, (int)mem.Length);
                header[3] = _sequence++;

                stream.Write(header, 0, 7);
                mem.Position = 0;
                mem.CopyTo(stream);
            }
        }

        private static void WriteInt3(byte[] buffer, int position, int value)
        {
            buffer[position] = (byte) value;
            buffer[position + 1] = (byte)(value >> 8);
            buffer[position + 2] = (byte)(value >> 16);
        }
    }
}
