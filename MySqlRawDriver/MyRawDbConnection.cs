using System;
using System.Data;

namespace MySqlRawDriver
{
    public class MyRawDbConnection : IDbConnection
    {
        private MyRawNativeConnection _native;

        public string ConnectionString { get; set; }
        public int ConnectionTimeout { get; set; }
        public string Database => _native.Options.Database;
        public ConnectionState State => _native.State;
        public MyRawNativeConnection NativeConnection => _native;

        public MyRawDbConnection()
        {
            _native = new MyRawNativeConnection();
        }

        public MyRawDbConnection(string connectionString) : this()
        {
            ConnectionString = connectionString;
        }

        public void Dispose()
        {
            _native?.Disconnect();
            _native = null;
        }

        public IDbTransaction BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            Dispose();
        }

        public void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public IDbCommand CreateCommand()
        {
            return new MyRawDbCommand(this);
        }

        public void Open()
        {
            ParseConnectionString(_native, ConnectionString);
            _native.Connect();
        }

        private static void ParseConnectionString(MyRawNativeConnection native, string connection)
        {
            var fields = connection.Split(';');
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field) || !field.Contains("="))
                    continue;

                var x = field.Split(new[] { '=' }, 2);
                var key = x[0].ToLower().Trim();
                var value = x[1].Trim();

                switch (key)
                {
                    case "connect timeout":
                    case "connection timeout":
                    case "connectiontimeout":
                        native.Options.ConnectTimeout = int.Parse(value);
                        break;

                    case "default command timeout":
                    case "defaultcommandtimeout":
                        native.Options.CommandTimeout = int.Parse(value);
                        break;

                    case "host":
                    case "server":
                    case "data source":
                    case "datasource":
                    case "address":
                    case "addr":
                    case "network address":
                        native.Options.Server = value;
                        break;

                    case "initial catalog":
                    case "database":
                        native.Options.Database = value;
                        break;

                    case "password":
                    case "pwd":
                        native.Options.Password = value;
                        break;

                    case "port":
                        native.Options.Port = ushort.Parse(value);
                        break;

                    case "user id":
                    case "userid":
                    case "username":
                    case "uid":
                    case "user name":
                    case "user":
                        native.Options.User = value;
                        break;

                    default:
                        throw new MyRawException("Connection string statement 'field' is unsupported.");
                }
            }
        }
    }
}
