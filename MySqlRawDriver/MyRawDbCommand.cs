using System;
using System.Data;

namespace MySqlRawDriver
{
    public class MyRawDbCommand : IDbCommand
    {
        private readonly MyRawNativeConnection _native;
        private readonly MyRawDbConnection _connection;

        public IDbConnection Connection { get; set; }
        public IDbTransaction Transaction { get; set; }
        public string CommandText { get; set; }
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; } = CommandType.Text;
        public IDataParameterCollection Parameters { get; } = new MyRawDbParameterCollection();
        public UpdateRowSource UpdatedRowSource { get; set; }

        internal MyRawDbCommand(MyRawDbConnection connection)
        {
            _connection = connection;
            _native = connection.NativeConnection;
        }

        public void Dispose()
        {
        }

        public void Prepare()
        {
        }

        public void Cancel()
        {
        }

        public IDbDataParameter CreateParameter()
        {
            var result = new MyRawDbParameter();
            Parameters.Add(result);
            return result;
        }

        public int ExecuteNonQuery()
        {
            throw new NotImplementedException();
        }

        public IDataReader ExecuteReader()
        {
            return new MyRawDbReader(_native.QueryMultiple(PrepareQuery()));
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return ExecuteReader();
        }

        public object ExecuteScalar()
        {
            return _native.QueryScalar(PrepareQuery());
        }

        private string PrepareQuery()
        {
            switch (CommandType)
            {
                case CommandType.StoredProcedure:
                    return "call " + CommandText + "()";
                case CommandType.TableDirect:
                    return "select * from " + CommandText;
                case CommandType.Text:
                    return CommandText;
                default:
                    throw new InvalidOperationException("Invalid command type.");
            }
        }
    }
}
