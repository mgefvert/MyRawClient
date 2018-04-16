using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyRawClient.Internal;

namespace MyRawClientTest.Internal
{
    [TestClass]
    public class HelperTest
    {
        [TestMethod]
        public void TestParseConnectionString()
        {
            var options = new Options();
            Helper.ParseConnectionString("server=127.0.0.1;uid=root;pwd=12345;database=test", options);
            Assert.AreEqual("127.0.0.1", options.Server);
            Assert.AreEqual("root", options.User);
            Assert.AreEqual("12345", options.Password);
            Assert.AreEqual("test", options.Database);
            Assert.AreEqual(3306, options.Port);

            options = new Options();
            Helper.ParseConnectionString("Data Source=db-host;Initial Catalog=dbname;User Id=user;Password=pass;Port=3307", options);
            Assert.AreEqual("db-host", options.Server);
            Assert.AreEqual("user", options.User);
            Assert.AreEqual("pass", options.Password);
            Assert.AreEqual("dbname", options.Database);
            Assert.AreEqual(3307, options.Port);
        }

        [TestMethod]
        public void TestPrepareQuery()
        {
            var parameters = new ParameterCollection
            {
                new Parameter { ParameterName = "name", Value = "Mikey" },
                new Parameter { ParameterName = "age", Value = 27 },
                new Parameter { ParameterName = "ts", Value = new DateTime(2018, 1, 9) },
                new Parameter { ParameterName = "id", Value = 3490 },
                new Parameter { ParameterName = "preference", Value = null }
            };

            var sql = Helper.PrepareQuery("insert into users values (@id, @name, @age, @ts, @preference)", parameters);
            Assert.AreEqual("insert into users values (3490, 'Mikey', 27, '2018-01-09', null)", sql);
        }

        [TestMethod]
        public void TestQuoteIdentifier()
        {
            Assert.AreEqual("`test`", Helper.QuoteIdentifier("test"));
            Assert.AreEqual("`test-table`", Helper.QuoteIdentifier("`test`-table"));
            Assert.AreEqual("`test-table`", Helper.QuoteIdentifier("'test'-table"));
            Assert.AreEqual("`table`.`field`", Helper.QuoteIdentifier("table", "field"));
            Assert.AreEqual("`table.field`", Helper.QuoteIdentifier("table.field"));
        }

        [TestMethod]
        public void TestQuoteString()
        {
            Assert.AreEqual("'test'", Helper.QuoteString("test"));
            Assert.AreEqual("'\\'test\\''", Helper.QuoteString("'test'"));
            Assert.AreEqual("'\\\"test\\\"'", Helper.QuoteString("\"test\""));
            Assert.AreEqual("'bobby tables\\'; --'", Helper.QuoteString("bobby tables'; --"));
            Assert.AreEqual("'null\\0'", Helper.QuoteString("null\0"));
            Assert.AreEqual("'lots of nulls\\0\\0\\0'", Helper.QuoteString("lots of nulls\0\0\0"));
        }

        [TestMethod]
        public void TestValueToSQL()
        {
            Assert.AreEqual("null", Helper.ValueToSQL(null));
            Assert.AreEqual("''", Helper.ValueToSQL(""));
            Assert.AreEqual("'string'", Helper.ValueToSQL("string"));
            Assert.AreEqual("'this is a \\\"string\\\" with \\'quotes\\''", Helper.ValueToSQL("this is a \"string\" with 'quotes'"));
            Assert.AreEqual("9", Helper.ValueToSQL(9));
            Assert.AreEqual("-5", Helper.ValueToSQL(-5));
            Assert.AreEqual("3.14159", Helper.ValueToSQL(3.14159));
            Assert.AreEqual("4099.50", Helper.ValueToSQL(4099.50M));
            Assert.AreEqual("-3E+19", Helper.ValueToSQL(-3.0e19));
            Assert.AreEqual("'2018-03-14'", Helper.ValueToSQL(new DateTime(2018, 3, 14)));
            Assert.AreEqual("'2018-03-14 15:09:26'", Helper.ValueToSQL(new DateTime(2018, 3, 14, 15, 9, 26)));
            Assert.AreEqual("'2018-03-14 15:09:26.535'", Helper.ValueToSQL(new DateTime(2018, 3, 14, 15, 9, 26, 535)));

            // I wonder if this will be flagged by antivirus programs.
            Assert.AreEqual("x'EA0000FFFF'", Helper.ValueToSQL(new byte[] { 0xEA, 0x00, 0x00, 0xFF, 0xFF }));
        }
    }
}
