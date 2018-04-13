using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlRawDriver.Auth;

namespace MySqlRawTest.Auth
{
    [TestClass]
    public class NativePasswordTest
    {
        [TestMethod]
        public void Test()
        {
            var password = Encoding.ASCII.GetBytes("random_password");
            var seed = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };

            Assert.AreEqual("145db9d431bc9c5958ffe0379d235ab86e5df00424", 
                NativePassword.Encrypt(password, seed).Aggregate("", (s, b) => s += b.ToString("x2")));
        }
    }
}
