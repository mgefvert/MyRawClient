# MyRawClient

Fast, lightweight client for MySQL databases based on the raw, native protocol.

Features a very lightweight implementation of the MySQL binary protocol (mostly the text-based query commands)
with a thin IDbConnection wrapper around it.

By carefully avoiding memory allocations as far as possible, there is a substantial speed increase compared to
the official MySQL .NET connector. Loading 1.7 million rows (two strings, two ints per row) on a reference machine yields
a ~4x speed increase (about 2 seconds vs roughly 8 seconds with the original library).

The library is considered roughly alpha stage stability with unit tests. It's currently built against .NET 4.6.

## Features

Implemented features:
* Run queries against the MySQL server with or without result sets
* Multi-result sets always handled
* Thin wrapper around IDbTransaction with BEGIN/COMMIT commands
* Emulates parameter support by parsing the query and substituting parameters
* Dapper support
* Standard IDbConnection / IDbReader interfaces supported

Not supported:
* Cursors
* Proper in/out parameter bindings
* Prepared statements
* Authentication schemes are limited to native mysql passwords for now
* SSL/TLS
* Compressed streams
* Nonbuffered reading
* .NET Core

(any of this may be implemented in the future)

## Examples

These examples are written using the native MyRawConnection methods. You can also
use the normal IDbCommand / IDbReader methods, but they add so much complexity for
little gain.

Run single query:

    var set = db.Query("select * from names");
    Console.WriteLine("Query row count = " + set.RowCount);
    Console.WriteLine("First five rows:");
    for (var i = 0; i < 5; i++)
        Console.WriteLine($"{set.Get<string>(i, 0)} ({set.Get<string>(i, 1)}) - {set.Get<int>(i, 2)}");

Query scalar:

    var time = db.QueryScalar<DateTime>("select now()");
    Console.WriteLine("Time = " + time);

Query multiple results:

    var results = db.QueryMultiple("select now(); select database(); select 42;");
    Console.WriteLine("Time = " + results[0].Get<DateTime>(0, 0));
    Console.WriteLine("Database = " + results[1].Get<string>(0, 0));
    Console.WriteLine("Supposedly random number = " + results[2].Get<int>(0, 0));

Use Dapper:

    var names = db.Query<NamePopularity>("select * from names order by count desc limit 5").ToList();
    foreach (var name in names)
        Console.WriteLine($"{name.Name} ({name.Sex}) - {name.Count}");

Insert row with Dapper parameterized query:

    db.Execute("insert into scratch (text) values (@text)", new { text = "Hello, world!" });
    Console.WriteLine("Row inserted with ID=" + db.LastInsertId);

Execute command:

    db.Execute("delete from scratch");
    Console.WriteLine("All rows deleted, rows affected=" + db.RowsAffected);
