using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlRawDriver;

namespace MySqlRawTest
{
    [TestClass]
    public class MyRawConnectionTest
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
        public void TestBeginTransaction()
        {
        }

        [TestMethod]
        public void TestClose()
        {
            Assert.AreEqual(ConnectionState.Open, _connection.State);
            _connection.Close();
            Assert.AreEqual(ConnectionState.Closed, _connection.State);
            _connection.Close();
            Assert.AreEqual(ConnectionState.Closed, _connection.State);
        }

        [TestMethod]
        public void TestChangeDatabase()
        {
            Assert.AreEqual(Global.DatabaseName, _connection.Database);
            _connection.ChangeDatabase("information_schema");
            Assert.AreEqual("information_schema", _connection.Database);
        }

        [TestMethod]
        public void TestCreateCommand()
        {
        }

        [TestMethod]
        public void TestExecute()
        {
            var result = _connection.Execute("insert into scratch (text) values ('borkbork')");
            Assert.AreEqual(1, result.RowsAffected);
            var id = result.LastInsertId;
            Assert.AreNotEqual(0, id);

            Assert.AreEqual("borkbork", _connection.QueryScalar<string>("select text from scratch where id=" + id));

            result = _connection.Execute("delete from scratch where id=" + id);
            Assert.AreEqual(1, result.RowsAffected);

            Assert.IsNull(_connection.QueryScalar<string>("select text from scratch where id=" + id));
        }

        [TestMethod]
        public void TestOpen()
        {
            Assert.AreEqual(ConnectionState.Open, _connection.State);
            Assert.AreEqual(Global.DatabaseName, _connection.Database);
        }

        [TestMethod]
        public void TestPing()
        {
            _connection.Ping();
            Assert.AreEqual(ConnectionState.Open, _connection.State);
        }

        [TestMethod]
        public void TestQuery()
        {
            var result = _connection.Query("select * from names where name like 'Jere%' order by name");
            Assert.AreEqual(3, result.FieldCount);
            Assert.AreEqual(3, result.Fields.Count);
            Assert.AreEqual("name", result.Fields[0].Name);
            Assert.AreEqual("sex", result.Fields[1].Name);
            Assert.AreEqual("count", result.Fields[2].Name);

            Assert.AreEqual(2, result.RowCount);
            Assert.AreEqual("Jeremiah", result.GetString(0, 0));
            Assert.AreEqual("Jeremy", result.GetString(1, 0));
            Assert.AreEqual("M", result.GetString(0, 1));
            Assert.AreEqual("M", result.GetString(1, 1));
            Assert.AreEqual(177912, result.Get<int>(0, 2));
            Assert.AreEqual(430070, result.Get<int>(1, 2));
        }

        [TestMethod]
        public void TestQueryMultiple()
        {
            var result = _connection.QueryMultiple("select database(); select 42; select curdate();");

            Assert.AreEqual(3, result.Count);

            Assert.AreEqual(Global.DatabaseName, result[0].GetString(0, 0));
            Assert.AreEqual(42, result[1].Get<int>(0, 0));
            Assert.AreEqual(DateTime.Today, result[2].Get<DateTime>(0, 0));
        }

        [TestMethod]
        public void TestQueryScalar()
        {
            Assert.AreEqual(Global.DatabaseName, _connection.QueryScalar("select database()"));
        }

        [TestMethod]
        public void TestResetConnection()
        {
            Assert.AreEqual(Global.DatabaseName, _connection.QueryScalar("select database()"));
            _connection.ResetConnection();
            Assert.AreEqual(Global.DatabaseName, _connection.QueryScalar("select database()"));
        }
    }
}
