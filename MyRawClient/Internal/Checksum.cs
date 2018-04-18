using System;
using System.Linq;

namespace MyRawClient.Internal
{
    public static class Checksum
    {
        private const uint Adler32Modulo = 65521;
        private static readonly uint[] Crc32Data = new uint[256];
        private const uint Crc32Polynomial = 0xEDB88320;

        static Checksum()
        {
            for (uint i = 0; i < Crc32Data.Length; ++i)
            {
                var temp = i;
                for (var j = 8; j > 0; --j)
                    if ((temp & 1) == 1)
                        temp = (temp >> 1) ^ Crc32Polynomial;
                    else
                        temp >>= 1;

                Crc32Data[i] = temp;
            }
        }

        public static uint Adler32(byte[] bytes)
        {
            uint a = 1, b = 0;
    
            foreach (var x in bytes)
            {
                a = (a + x) % Adler32Modulo;
                b = (b + a) % Adler32Modulo;
            }
    
            return (b << 16) | a;
        }

        public static uint Crc32(byte[] bytes)
        {
            return ~bytes.Aggregate(0xFFFF_FFFF, (c, t) => (c >> 8) ^ Crc32Data[(byte) ((c & 0xFF) ^ t)]);
        }
    }
}