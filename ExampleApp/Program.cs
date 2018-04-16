using System;
using System.Configuration;
using System.Linq;
using Dapper;
using MyRawClient;

namespace ExampleApp
{
    class Program
    {
        static void Main()
        {
            using (var db = new MyRawConnection(ConfigurationManager.ConnectionStrings["local"].ConnectionString))
            {
                db.Open();

                // Run single query
                var set = db.Query("select * from names");
                Console.WriteLine("Query row count = " + set.RowCount);
                Console.WriteLine("First five rows:");
                for (var i = 0; i < 5; i++)
                    Console.WriteLine($"{set.Get<string>(i, 0)} ({set.Get<string>(i, 1)}) - {set.Get<int>(i, 2)}");
                Console.WriteLine();

                // Query scalar
                var time = db.QueryScalar<DateTime>("select now()");
                Console.WriteLine("Time = " + time);
                Console.WriteLine();

                // Query multiple results
                var results = db.QueryMultiple("select now(); select database(); select 42;");
                Console.WriteLine("Time = " + results[0].Get<DateTime>(0, 0));
                Console.WriteLine("Database = " + results[1].Get<string>(0, 0));
                Console.WriteLine("Supposedly random number = " + results[2].Get<int>(0, 0));
                Console.WriteLine();

                // Use Dapper
                var names = db.Query<NamePopularity>("select * from names order by count desc limit 5").ToList();
                foreach (var name in names)
                    Console.WriteLine($"{name.Name} ({name.Sex}) - {name.Count}");
                Console.WriteLine();

                // Insert row with Dapper parameterized query
                db.Execute("insert into scratch (text) values (@text)", new { text = "Hello, world!" });
                Console.WriteLine("Row inserted with ID=" + db.LastInsertId);
                Console.WriteLine();

                // Execute command
                db.Execute("delete from scratch");
                Console.WriteLine("All rows deleted, rows affected=" + db.RowsAffected);
                Console.WriteLine();
            }
        }
    }
}
