using System;
using System.Data;

namespace MySqlRawDriver
{
    public class MyRawDbTransaction : IDbTransaction
    {
        public IDbConnection Connection { get; }
        public IsolationLevel IsolationLevel { get; } = IsolationLevel.Unspecified;
        private bool _active = true;

        public MyRawDbTransaction(MyRawDbConnection connection)
        {
            Connection = connection;
            connection.Execute("start transaction");
        }

        public void Dispose()
        {
            if (_active)
                Rollback();
        }

        public void Commit()
        {
            if (!_active)
                throw new DataException("Cannot commit transaction; no transaction is currently active.");

            _active = false;
            ((MyRawDbConnection) Connection).Execute("commit");
        }

        public void Rollback()
        {
            if (!_active)
                throw new DataException("Cannot commit transaction; no transaction is currently active.");

            _active = false;
            ((MyRawDbConnection) Connection).Execute("rollback");
        }
    }
}
