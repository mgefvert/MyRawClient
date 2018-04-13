using System;
using System.Data;

namespace MySqlRawDriver
{
    public class MyRawDbCommand : IDbCommand
    {
        private MyRawDbConnection _connection;

        public IDbTransaction Transaction { get; set; }
        public string CommandText { get; set; }
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; } = CommandType.Text;
        public IDataParameterCollection Parameters { get; } = new MyRawDbParameterCollection();
        public UpdateRowSource UpdatedRowSource { get; set; }

        public IDbConnection Connection
        {
            get => _connection;
            set => _connection = (MyRawDbConnection)value;
        }

        internal MyRawDbCommand(MyRawDbConnection connection)
        {
            _connection = connection;
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
            return new MyRawDbParameter();
        }

        public int ExecuteNonQuery()
        {
            return (int)_connection.Execute(PrepareQuery()).RowsAffected;
        }

        public IDataReader ExecuteReader()
        {
            return new MyRawDbReader(_connection.QueryMultiple(PrepareQuery()));
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return ExecuteReader();
        }

        public object ExecuteScalar()
        {
            return _connection.QueryScalar(PrepareQuery());
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
                    return MyRawHelper.PrepareQuery(CommandText, Parameters);
                default:
                    throw new InvalidOperationException("Invalid command type.");
            }
        }
    }
}
