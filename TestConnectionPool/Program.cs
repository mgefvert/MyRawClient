using System;
using System.Configuration;
using MyRawClient;

namespace TestConnectionPool
{
    class Program
    {
        private static MyRawConnection[] _connections;

        static void Main(string[] args)
        {
            _connections = new MyRawConnection[5];
            MyRawConnectionPool.Debug = true;

            Console.WriteLine("These should not be pooled.");
            for (var i = 0; i < _connections.Length; i++)
            {
                _connections[i] = GetConnection();
                ShowConnection(i);
            }
            Console.WriteLine();
            Console.WriteLine("Press ENTER to continue");
            Console.ReadLine();

            _connections[1].Close();
            _connections[3].Close();

            Console.WriteLine("Odd ones should be pooled.");
            for (var i = 0; i < _connections.Length; i++)
            {
                _connections[i].Open();
                ShowConnection(i);
            }
            Console.WriteLine();
            Console.WriteLine("Press ENTER to continue");
            Console.ReadLine();

            Console.WriteLine("Closing all connections");
            foreach (var c in _connections)
                c.Close();
            Console.WriteLine();
            Console.WriteLine("Press ENTER to continue");
            Console.ReadLine();

            Console.WriteLine("These should all be pooled.");
            for (var i = 0; i < _connections.Length; i++)
            {
                _connections[i] = GetConnection();
                ShowConnection(i);
            }
            Console.WriteLine();
            Console.WriteLine("Press ENTER to continue");
            Console.ReadLine();

            Console.WriteLine("Closing all connections");
            foreach (var c in _connections)
                c.Close();
            Console.WriteLine();

            Console.WriteLine("--> Waiting for pool debug activity, press ENTER to finish and close the program");
            Console.ReadLine();
        }

        public static MyRawConnection GetConnection()
        {
            var connection = new MyRawConnection(ConfigurationManager.ConnectionStrings["local"].ConnectionString);
            connection.Open();
            return connection;
        }

        public static void ShowConnection(int i)
        {
            Console.WriteLine($"Connection {i}: pooled={_connections[i].Pooled}, state={_connections[i].State}, ID={_connections[i].ServerInfo.ConnectionId}, time=" +
                              _connections[i].QueryScalar<DateTime>("select now()"));
        }
    }
}
