using System;
using System.Security.Cryptography;

namespace MySqlRawDriver.Auth
{
    public static class NativePassword
    {
        public static byte[] Encrypt(byte[] password, byte[] seedBytes)
        {
            if (password.Length == 0) 
                return new byte[1];

            SHA1 sha = new SHA1CryptoServiceProvider();

            var firstHash = sha.ComputeHash(password);
            var secondHash = sha.ComputeHash(firstHash);

            var input = new byte[seedBytes.Length + secondHash.Length];
            Array.Copy(seedBytes, 0, input, 0, seedBytes.Length);
            Array.Copy(secondHash, 0, input, seedBytes.Length, secondHash.Length);
            var thirdHash = sha.ComputeHash(input);

            var finalHash = new byte[thirdHash.Length + 1];
            finalHash[0] = 0x14;
            Array.Copy(thirdHash, 0, finalHash, 1, thirdHash.Length);

            for (var i = 1; i < finalHash.Length; i++)
                finalHash[i] = (byte)(finalHash[i] ^ firstHash[i - 1]);

            return finalHash;
        }
    }
}
