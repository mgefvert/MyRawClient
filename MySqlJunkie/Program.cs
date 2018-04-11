using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using MySqlRawDriver;

// ReSharper disable LocalizableElement

namespace MySqlJunkie
{
    public class NameOfYear
    {
        public string Name { get; set; }
        public string Sex { get; set; }
        public int Year { get; set; }
        public int Count { get; set; }
    }

    class Program
    {
        static void Main()
        {
            //TestNative();
            TestDbConnection();
        }

        public static void TestDbConnection()
        {
            using (var db = new MyRawDbConnection("User=root; Password=sm0gen; Database=data; Server=localhost;"))
            {
                db.Open();

                var t0 = DateTime.Now;
                var result = db.Query<NameOfYear>("select * from names_by_year").ToList();
                Console.WriteLine($"Total fetch in {(int)(DateTime.Now - t0).TotalMilliseconds}, count={result.Count}");
            }
        }

        public static void TestNative()
        {
            var c = new MyRawNativeConnection
            {
                Options = {
                    User = "root",
                    Password = "sm0gen",
                    Server = "localhost",
                    Database = "data"
                }
            };
            c.Connect();

            c.Ping();
            Console.WriteLine("* Got ping *");

            var ok = c.Execute("insert into test (name, title) values ('bam-bam', 19)");
            Console.WriteLine($"Inserted, affected={ok.AffectedRows} lastId={ok.LastInsertId}");

            Dump(c.QueryMultiple("select now(); select rand()"));

            var t0 = DateTime.Now;
            var result = c.Query("select * from names_by_year");

            var sum = 0;
            var len = 0;
            for (int i = 0; i < result.RowCount; i++)
            {
                len += result.GetString(i, 0).Length + result.GetString(i, 1).Length;
                sum += result.Get<int>(i, 2) + result.Get<int>(i, 3);
            }
            Console.WriteLine($"Total fetch in {(int)(DateTime.Now - t0).TotalMilliseconds}, len={len} sum={sum}");

            Console.WriteLine("Time = " + c.QueryScalar("select now()"));

            c.ResetConnection();
            Console.WriteLine("* Connection reset");

            Console.WriteLine("Time = " + c.QueryScalar("select now()"));

            c.Ping();
            Console.WriteLine("* Got ping *");
        }

        private static void Dump(List<MyRawResultSet> results)
        {
            foreach (var result in results)
                Dump(result);
        }

        private static void Dump(MyRawResultSet result)
        {
            foreach (var field in result.Fields)
                Console.Write(" | " + field.Name);
            Console.WriteLine();

            for (var i = 0; i < result.RowCount; i++)
            {
                for (var j = 0; j < result.ColumnCount; j++)
                    Console.Write(" | " + result.GetString(i, j));

                Console.WriteLine();
            }

            Console.WriteLine();
        }
    }
}
