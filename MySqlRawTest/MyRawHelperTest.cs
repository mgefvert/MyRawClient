using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlRawDriver;

namespace MySqlRawTest
{
    [TestClass]
    public class MyRawHelperTest
    {
        [TestMethod]
        public void TestValueToSQL()
        {
            Assert.AreEqual("null", MyRawHelper.ValueToSQL(null));
            Assert.AreEqual("''", MyRawHelper.ValueToSQL(""));
            Assert.AreEqual("'string'", MyRawHelper.ValueToSQL("string"));
            Assert.AreEqual("'this is a \\\"string\\\" with \\'quotes\\''", MyRawHelper.ValueToSQL("this is a \"string\" with 'quotes'"));
            Assert.AreEqual("9", MyRawHelper.ValueToSQL(9));
            Assert.AreEqual("-5", MyRawHelper.ValueToSQL(-5));
            Assert.AreEqual("3.14159", MyRawHelper.ValueToSQL(3.14159));
            Assert.AreEqual("4099.50", MyRawHelper.ValueToSQL(4099.50M));
            Assert.AreEqual("-3E+19", MyRawHelper.ValueToSQL(-3.0e19));
            Assert.AreEqual("'2018-03-14'", MyRawHelper.ValueToSQL(new DateTime(2018, 3, 14)));
            Assert.AreEqual("'2018-03-14 15:09:26'", MyRawHelper.ValueToSQL(new DateTime(2018, 3, 14, 15, 9, 26)));
            Assert.AreEqual("'2018-03-14 15:09:26.535'", MyRawHelper.ValueToSQL(new DateTime(2018, 3, 14, 15, 9, 26, 535)));

            // I wonder if this will be flagged by antivirus programs.
            Assert.AreEqual("x'EA0000FFFF'", MyRawHelper.ValueToSQL(new byte[] { 0xEA, 0x00, 0x00, 0xFF, 0xFF }));
        }
    }
}
