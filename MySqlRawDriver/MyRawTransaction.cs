using System;
using System.Data;

namespace MySqlRawDriver
{
    public class MyRawTransaction : IDbTransaction
    {
        public IDbConnection Connection { get; }
        public IsolationLevel IsolationLevel { get; } = IsolationLevel.Unspecified;
        private bool _active = true;

        public MyRawTransaction(MyRawConnection connection)
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
            ((MyRawConnection) Connection).Execute("commit");
        }

        public void Rollback()
        {
            if (!_active)
                throw new DataException("Cannot commit transaction; no transaction is currently active.");

            _active = false;
            ((MyRawConnection) Connection).Execute("rollback");
        }
    }
}
