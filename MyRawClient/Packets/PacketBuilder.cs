using System;
using System.IO;
using System.Text;
using MyRawClient.Enumerations;

namespace MyRawClient.Packets
{
    /// <summary>
    /// Helper class for building MySQL packets. Writes information to a MemoryStream which can then
    /// be saved. Does not include the four-byte header.
    /// </summary>
    public class PacketBuilder : IDisposable
    {
        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;
        private readonly Encoding _encoding;

        public PacketBuilder(Encoding encoding)
        {
            _encoding = encoding;
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream, encoding);
        }

        public void Dispose()
        {
            _writer.Dispose();
            _stream.Dispose();
        }

        public static byte[] MakeCommand(Commands command)
        {
            return new[] { (byte)command };
        }

        public byte[] ToPacket()
        {
            return _stream.ToArray();
        }

        public void AppendInt1(byte value)
        {
            _writer.Write(value);
        }

        public void AppendInt2(ushort value)
        {
            _writer.Write(value);
        }

        public void AppendInt3(uint value)
        {
            _writer.Write((byte)value);
            _writer.Write((byte)(value >> 8));
            _writer.Write((byte)(value >> 16));
        }

        public void AppendInt4(uint value)
        {
            _writer.Write(value);
        }

        public void AppendInt6(ulong value)
        {
            _writer.Write((uint)value);
            _writer.Write((ushort)(value >> 32));
        }

        public void AppendInt8(ulong value)
        {
            _writer.Write(value);
        }

        public void AppendIntLengthEncoded(ulong value)
        {
            if (value < 251)
                AppendInt1((byte)value);
            else if (value < 65536)
            {
                _writer.Write((byte)0xFC);
                AppendInt2((ushort)value);
            }
            else if (value < 16777216)
            {
                _writer.Write((byte)0xFD);
                AppendInt3((uint)value);
            }
            else
            {
                _writer.Write((byte)0xFE);
                AppendInt8(value);
            }
        }

        public void AppendBytes(byte[] value)
        {
            _writer.Write(value);
        }

        public void AppendStringFixed(string value)
        {
            _writer.Write(_encoding.GetBytes(value));
        }

        public void AppendStringNullTerminated(string value)
        {
            _writer.Write(_encoding.GetBytes(value));
            _writer.Write((byte)0);
        }

        public void AppendStringLengthEncoded(string value)
        {
            AppendIntLengthEncoded((uint)value.Length);
            AppendStringFixed(value);
        }
    }
}
