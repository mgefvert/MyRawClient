using System;
using System.Linq;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyRawClient;

namespace MyRawClientTest
{
    public class NamePopularity
    {
        public string Name { get; set; }
        public string Sex { get; set; }
        public int Count { get; set; }
    }

    public class Scratch
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public int? Number { get; set; }
        public decimal? Money { get; set; }
        public DateTime? Ts { get; set; }
    }

    [TestClass]
    public class DapperTest
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
        public void TestQuery()
        {
            var result = _connection.Query<NamePopularity>("select * from names where name like 'Jere%' order by name").ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Jeremiah", result[0].Name);
            Assert.AreEqual("Jeremy", result[1].Name);
            Assert.AreEqual("M", result[0].Sex);
            Assert.AreEqual("M", result[1].Sex);
            Assert.AreEqual(177912, result[0].Count);
            Assert.AreEqual(430070, result[1].Count);
        }

        [TestMethod]
        public void TestQueryMultiple()
        {
            var result = SqlMapper.QueryMultiple(_connection, "select database(); select 42; select curdate();");

            Assert.AreEqual(Global.DatabaseName, result.ReadSingle<string>());
            Assert.AreEqual(42, result.ReadSingle<int>());
            Assert.AreEqual(DateTime.Today, result.ReadSingle<DateTime>());
        }

        [TestMethod]
        public void TestQueryParameters()
        {
            var result = _connection.Query<NamePopularity>("select * from names where count=@count", new { count = 177912 }).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Jeremiah", result[0].Name);
            Assert.AreEqual("M", result[0].Sex);
            Assert.AreEqual(177912, result[0].Count);
        }

        [TestMethod]
        public void TestQueryParametersComplex()
        {
            var record = new Scratch
            {
                Money = 45.22M,
                Number = null,
                Text = "Hello, world",
                Ts = new DateTime(2018, 3, 14, 15, 9, 26)
            };

            _connection.Execute("insert into scratch (text, number, money, ts) values (@text, @number, @money, @ts)", record);
            Assert.AreEqual(1, (int)_connection.RowsAffected);
            record.Id = (int)_connection.LastInsertId;

            var result = _connection.Query<Scratch>("select * from scratch where id=@id", new { id = record.Id }).Single();
            Assert.AreEqual(result.Id, record.Id);
            Assert.AreEqual(result.Money, record.Money);
            Assert.AreEqual(result.Number, record.Number);
            Assert.AreEqual(result.Text, record.Text);
            Assert.AreEqual(result.Ts, record.Ts);

            _connection.Execute("delete from scratch where id=@id", new { record.Id });
            Assert.AreEqual(1, (int)_connection.RowsAffected);
        }

        [TestMethod]
        public void TestQueryParametersList()
        {
            var result = _connection.Query<NamePopularity>("select * from names where name in @names and sex='M'", 
                new { names = new[] { "James", "John", "Michael", "Robert", "William" }}).ToList();

            Assert.AreEqual(23415305, result.Sum(x => x.Count));
        }
    }
}
