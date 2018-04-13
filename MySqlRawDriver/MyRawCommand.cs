using System;
using System.Data;
using MySqlRawDriver.Internal;

namespace MySqlRawDriver
{
    public class MyRawCommand : IDbCommand
    {
        private MyRawConnection _connection;

        public IDbTransaction Transaction { get; set; }
        public string CommandText { get; set; }
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; } = CommandType.Text;
        public IDataParameterCollection Parameters { get; } = new ParameterCollection();
        public UpdateRowSource UpdatedRowSource { get; set; }

        public IDbConnection Connection
        {
            get => _connection;
            set => _connection = (MyRawConnection)value;
        }

        internal MyRawCommand(MyRawConnection connection)
        {
            _connection = connection;
        }

        public void Dispose()
        {
        }

        public void Cancel()
        {
        }

        public IDbDataParameter CreateParameter()
        {
            return new Parameter();
        }

        public int ExecuteNonQuery()
        {
            return (int)_connection.Execute(PrepareQuery()).RowsAffected;
        }

        public IDataReader ExecuteReader()
        {
            return new MyRawReader(_connection.QueryMultiple(PrepareQuery()));
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return ExecuteReader();
        }

        public object ExecuteScalar()
        {
            return _connection.QueryScalar(PrepareQuery());
        }

        public void Prepare()
        {
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
                    return Helper.PrepareQuery(CommandText, Parameters);
                default:
                    throw new InvalidOperationException("Invalid command type.");
            }
        }
    }
}
