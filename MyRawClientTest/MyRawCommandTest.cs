using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyRawClient;

namespace MyRawClientTest
{
    [TestClass]
    public class MyRawCommandTest
    {
        private MyRawConnection _connection;

        [TestInitialize]
        public void Setup()
        {
            _connection = Global.GetConnection();
        }

        [TestCleanup]
        public void Teardown()
        {
            _connection.Dispose();
        }

        [TestMethod]
        public void TestExecuteNonQuery()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select 42";
                Assert.AreEqual(0, cmd.ExecuteNonQuery());
            }
        }

        [TestMethod]
        public void TestExecuteReader()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select 42";

                using (var reader = cmd.ExecuteReader())
                {
                    Assert.IsTrue(reader.Read());
                    Assert.AreEqual(42L, reader.GetValue(0));

                    Assert.IsFalse(reader.Read());
                }
            }
        }

        [TestMethod]
        public void TestExecuteScalar()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select 42";
                Assert.AreEqual(42L, cmd.ExecuteScalar());
            }
        }

        [TestMethod]
        public void TestCommandTypeTable()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandType = CommandType.TableDirect;
                cmd.CommandText = "names";

                using (var reader = cmd.ExecuteReader())
                {
                    var count = 0;
                    while (reader.Read())
                        count++;

                    Assert.AreEqual(2489, count);
                }
            }
        }

        [TestMethod]
        public void TestCommandTypeStoredProcedure()
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "supposedly_random";

                Assert.AreEqual(42L, cmd.ExecuteScalar());
            }
        }
    }
}
