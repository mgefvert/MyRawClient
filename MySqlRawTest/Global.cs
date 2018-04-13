using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySqlRawDriver;

namespace MySqlRawTest
{
    [TestClass]
    public class Global
    {
        public const string DatabaseName = "mysqlrawdriver";

        public static MyRawConnection GetConnection()
        {
            var db = new MyRawConnection(ConfigurationManager.ConnectionStrings["local"].ConnectionString);
            db.Open();
            return db;
        }

        public static string GetEmbeddedFile(string name)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Global), name))
            {
                if (stream == null)
                    throw new MissingManifestResourceException(name + " not found");

                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        [AssemblyInitialize]
        public static void InitializeDatabase(TestContext context)
        {
            using (var db = GetConnection())
            {
                db.Execute(GetEmbeddedFile("init.sql"));
                Thread.Sleep(200); // Apparently MySQL needs a little time to figure out there are now tables?
            }
        }

        [TestMethod]
        public void Test()
        {
            using (var db = GetConnection())
            {
                Assert.IsNotNull(db.ConnectionString);
            }
        }
    }
}
