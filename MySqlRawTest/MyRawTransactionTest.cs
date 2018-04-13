using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlRawDriver;

namespace MySqlRawTest
{
    [TestClass]
    public class MyRawTransactionTest
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
        public void TestCommit()
        {
            var transaction = _connection.BeginTransaction();
            _connection.Execute("insert into scratch (text) values ('johnny')");
            Assert.AreEqual(1, _connection.QueryScalar<int>("select count(*) from scratch where text = 'johnny'"));
            transaction.Commit();
            Assert.AreEqual(1, _connection.QueryScalar<int>("select count(*) from scratch where text = 'johnny'"));
            _connection.Execute("truncate scratch");
            Assert.AreEqual(0, _connection.QueryScalar<int>("select count(*) from scratch where text = 'johnny'"));
        }

        [TestMethod]
        public void TestRollback()
        {
            var transaction = _connection.BeginTransaction();
            _connection.Execute("insert into scratch (text) values ('johnny')");
            Assert.AreEqual(1, _connection.QueryScalar<int>("select count(*) from scratch where text = 'johnny'"));
            transaction.Rollback();
            Assert.AreEqual(0, _connection.QueryScalar<int>("select count(*) from scratch where text = 'johnny'"));
            _connection.Execute("truncate scratch");
            Assert.AreEqual(0, _connection.QueryScalar<int>("select count(*) from scratch where text = 'johnny'"));
        }
    }
}
