using System;
using System.Text;
using MySqlRawDriver.Enumerations;

namespace MySqlRawDriver.Packets
{
    public static class PacketReader
    {
        public static bool EndOfPacket(byte[] buffer, int position) => position >= buffer.Length;

        public static byte Command(byte[] buffer)
        {
            return buffer[0];
        }

        public static bool IsOkPacket(byte[] buffer)
        {
            return buffer[0] == (byte)ResultMode.Ok && buffer.Length > 3 || buffer[0] == (byte)ResultMode.Eof && buffer.Length <= 5;
        }

        public static bool IsErrPacket(byte[] buffer)
        {
            return buffer[0] == (byte)ResultMode.Error;
        }

        public static string PacketToString(byte[] buffer)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < buffer.Length; i++)
            {
                if (i == 0)
                    sb.Append("len=");
                else if (i == 3)
                    sb.Append(" seq=");
                else if (i == 4)
                    sb.Append(" data=");

                var b = buffer[i];
                if (b <= 32 || b >= 128)
                    sb.Append("\\" + b.ToString("X2"));
                else
                    sb.Append((char) b);

            }

            return sb.ToString();
        }

        // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
        private static void VerifyRemaining(byte[] buffer, int position, int remaining)
        {
            if (buffer.Length - position < remaining)
                throw new MyRawException("Out of data: tried to read beyond packet end.");
        }
        // ReSharper enable ParameterOnlyUsedForPreconditionCheck.Local

        public static bool ConsumeNull(byte[] buffer, ref int position)
        {
            VerifyRemaining(buffer, position, 1);
            if (buffer[position] != 0xFB)
                return false;

            position++;
            return true;
        }

        public static byte ReadInt1(byte[] buffer, ref int position)
        {
            VerifyRemaining(buffer, position, 1);
            return buffer[position++];
        }

        public static ushort ReadInt2(byte[] buffer, ref int position)
        {
            VerifyRemaining(buffer, position, 2);
            var result = BitConverter.ToUInt16(buffer, position);
            position += 2;
            return result;
        }

        public static uint ReadInt3(byte[] buffer, ref int position)
        {
            VerifyRemaining(buffer, position, 3);
            var result = (uint) BitConverter.ToUInt16(buffer, position);
            result |= (uint) buffer[position + 2] >> 16;
            position += 3;
            return result;
        }

        public static uint ReadInt4(byte[] buffer, ref int position)
        {
            VerifyRemaining(buffer, position, 4);
            var result = BitConverter.ToUInt32(buffer, position);
            position += 4;
            return result;
        }

        public static ulong ReadInt6(byte[] buffer, ref int position)
        {
            VerifyRemaining(buffer, position, 6);
            var result = (ulong) BitConverter.ToUInt32(buffer, position);
            result |= (ulong) BitConverter.ToUInt16(buffer, position + 4) >> 32;
            position += 6;
            return result;
        }

        public static ulong ReadInt8(byte[] buffer, ref int position)
        {
            VerifyRemaining(buffer, position, 8);
            var result = BitConverter.ToUInt64(buffer, position);
            position += 8;
            return result;
        }

        public static ulong ReadIntLengthEncoded(byte[] buffer, ref int position)
        {
            var value = ReadInt1(buffer, ref position);
            if (value < 251)
                return value;

            switch (value)
            {
                case 0xFC:
                    return ReadInt2(buffer, ref position);

                case 0xFD:
                    return ReadInt3(buffer, ref position);

                case 0xFE:
                    return ReadInt8(buffer, ref position);

                default:
                    throw new Exception("Invalid length-encoded integer " + value.ToString("X2"));
            }
        }

        public static byte[] ReadBytesFixed(byte[] buffer, ref int position, int length)
        {
            VerifyRemaining(buffer, position, length);
            var result = new byte[length];
            Array.Copy(buffer, position, result, 0, length);
            position += length;
            return result;
        }

        public static string ReadStringFixed(byte[] buffer, ref int position, int length, Encoding encoding)
        {
            VerifyRemaining(buffer, position, length);
            var result = encoding.GetString(buffer, position, length);
            position += length;
            return result;
        }

        public static string ReadStringLengthEncoded(byte[] buffer, ref int position, Encoding encoding)
        {
            var len = (int)ReadIntLengthEncoded(buffer, ref position);
            return ReadStringFixed(buffer, ref position, len, encoding);
        }

        public static string ReadStringNullTerminated(byte[] buffer, ref int position, Encoding encoding)
        {
            var origin = position;
            while (ReadInt1(buffer, ref position) != 0)
            {
            }

            return encoding.GetString(buffer, origin, position - origin - 1);
        }

        public static string ReadStringToEnd(byte[] buffer, ref int position, Encoding encoding)
        {
            var len = buffer.Length - position;
            return ReadStringFixed(buffer, ref position, len, encoding);
        }
    }
}
