using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlRawDriver.Internal;

namespace MySqlRawTest.Internal
{
    [TestClass]
    public class ParameterCollectionTest
    {
        private ParameterCollection _parameters;

        [TestInitialize]
        public void Setup()
        {
            _parameters = new ParameterCollection
            {
                new Parameter { ParameterName = "name", Value = "Mikey" },
                new Parameter { ParameterName = "age", Value = 27 },
                new Parameter { ParameterName = "ts", Value = new DateTime(2018, 1, 9) },
                new Parameter { ParameterName = "id", Value = 3490 },
                new Parameter { ParameterName = "preference", Value = null }
            };
        }

        [TestMethod]
        public void TestContains()
        {
            Assert.IsTrue(_parameters.Contains("name"));
            Assert.IsTrue(_parameters.Contains("Name"));
            Assert.IsTrue(_parameters.Contains("NAME"));
            Assert.IsFalse(_parameters.Contains(""));
            Assert.IsFalse(_parameters.Contains("none"));
            Assert.IsFalse(_parameters.Contains(null));
        }

        [TestMethod]
        public void TestIndexer()
        {
            Assert.AreEqual(3490, _parameters["id"]);
            _parameters["id"] = 999;
            Assert.AreEqual(999, _parameters["id"]);
            _parameters["id"] = null;
            Assert.AreEqual(null, _parameters["id"]);
        }

        [TestMethod]
        public void TestIndexOf()
        {
            Assert.AreEqual(0, _parameters.IndexOf("name"));
            Assert.AreEqual(3, _parameters.IndexOf("ID"));
            Assert.AreEqual(-1, _parameters.IndexOf(""));
            Assert.AreEqual(-1, _parameters.IndexOf("none"));
            Assert.AreEqual(-1, _parameters.IndexOf(null));
        }

        [TestMethod]
        public void TestRemoveAt()
        {
            Assert.AreEqual("name,age,ts,id,preference", string.Join(",", _parameters.Select(x => x.ParameterName)));
            _parameters.RemoveAt("ts");
            _parameters.RemoveAt("none");
            _parameters.RemoveAt(null);
            Assert.AreEqual("name,age,id,preference", string.Join(",", _parameters.Select(x => x.ParameterName)));
        }
    }
}
