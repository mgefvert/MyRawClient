using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MyRawClient.Enumerations;
using MyRawClient.Internal;
using MyRawClient.Packets;

namespace MyRawClient
{
    public class MyRawConnection : IDbConnection
    {
        public string ConnectionString { get; set; }
        public string Database { get; private set; }
        public Options Options { get; } = new Options();
        public bool Pooled { get; private set; }
        public Result Result { get; protected set; }

        public int ConnectionTimeout => Options.ConnectTimeout;
        public long LastInsertId => Result?.LastInsertId ?? 0;
        public long RowsAffected => Result?.RowsAffected ?? 0;
        public ConnectionState State => Connection?.State ?? ConnectionState.Closed;
        public ServerInfo ServerInfo => Connection?.ServerInfo;

        internal Connection Connection;

        public MyRawConnection()
        {
        }

        public MyRawConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }

        ~MyRawConnection()
        {
            Dispose();
        }

        public void Dispose()
        {
            Close();
        }


        // --- Internal methods ---

        protected void AssertConnected()
        {
            if (Connection == null || Connection.State == ConnectionState.Closed || Connection.State == ConnectionState.Broken)
                throw new MyRawException("Database connection is not open.");
        }

        protected List<ResultSet> DoQuery(string sql, int capacity = 256)
        {
            AssertConnected();
            Connection.AssertStateIs(ConnectionState.Open);
            Connection.State = ConnectionState.Executing;

            // Build and send query
            var builder = new PacketBuilder(Options.Encoding);
            builder.AppendInt1((byte)Commands.Query);
            builder.AppendStringFixed(sql);
            Connection.SendPacket(builder.ToPacket(), true);

            List<ResultSet> results = null;
            do
            {
                // Read response
                var packet = Connection.ReadPacket();

                // Execute queries with no result set
                if (PacketReader.IsOkPacket(packet))
                {
                    HandleOkPacket(packet);
                    Connection.State = ConnectionState.Open;
                    return results;
                }

                // Read column count and initialize reader
                var position = 0;
                var columnCount = (int) PacketReader.ReadIntLengthEncoded(packet, ref position);
                var resultSet = new ResultSet(columnCount, capacity, Options.Encoding);

                // Fetch column definitions
                Connection.State = ConnectionState.Fetching;
                for (var i = 0; i < columnCount; i++)
                    resultSet.Fields.Add(FetchColumnDefinition());

                HandleOkPacket(Connection.ReadPacket());

                // Fetch all data
                for (;;)
                {
                    packet = Connection.ReadPacket();
                    if (PacketReader.IsOkPacket(packet))
                    {
                        HandleOkPacket(packet);
                        break;
                    }

                    resultSet.AddBuffer(packet);
                }

                if (results == null)
                    results = new List<ResultSet>();
                results.Add(resultSet);
            } while (Result.Status.HasFlag(StatusFlags.MoreResultsExist));

            Connection.State = ConnectionState.Open;
            return results;
        }

        private ResultField FetchColumnDefinition()
        {
            var item = new ResultField();
            var packet = Connection.ReadPacket();
            var position = 0;

            item.Catalog = PacketReader.ReadStringLengthEncoded(packet, ref position, Options.Encoding);
            item.Schema = PacketReader.ReadStringLengthEncoded(packet, ref position, Options.Encoding);
            item.Table = PacketReader.ReadStringLengthEncoded(packet, ref position, Options.Encoding);
            item.OrgTable = PacketReader.ReadStringLengthEncoded(packet, ref position, Options.Encoding);
            item.Name = PacketReader.ReadStringLengthEncoded(packet, ref position, Options.Encoding);
            item.OrgName = PacketReader.ReadStringLengthEncoded(packet, ref position, Options.Encoding);

            PacketReader.ReadIntLengthEncoded(packet, ref position);

            item.CharSet = PacketReader.ReadInt2(packet, ref position);
            item.FieldLength = (int) PacketReader.ReadInt4(packet, ref position);
            item.DataType = (RawFieldType) PacketReader.ReadInt1(packet, ref position);
            item.Flags = (ColumnFlags) PacketReader.ReadInt2(packet, ref position);
            item.Decimals = PacketReader.ReadInt1(packet, ref position);

            item.InternalType = ResultField.DataTypeToInternalType(item.DataType, item.Flags);
            item.FieldType = ResultField.InternalTypeToType(item.InternalType);

            return item;
        }

        protected void HandleOkPacket(byte[] packet)
        {
            Result = Result.Decode(packet, Options.Encoding);
        }

        protected void UpdateDatabaseName()
        {
            Database = QueryScalar<string>("select database()");
        }


        // --- Public methods ---

        public IDbTransaction BeginTransaction()
        {
            return BeginTransaction(IsolationLevel.Unspecified);
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            AssertConnected();
            return new MyRawTransaction(this);
        }

        public void Close()
        {
            if (Connection != null)
                MyRawConnectionPool.Consume(Options.Key, Connection, null);

            Connection = null;
        }

        public void ChangeDatabase(string databaseName)
        {
            Execute("use " + QuoteIdentifier(databaseName));
            UpdateDatabaseName();
        }

        public IDbCommand CreateCommand()
        {
            AssertConnected();
            return new MyRawCommand(this);
        }

        public Result Execute(string sql)
        {
            DoQuery(sql);
            return Result;
        }

        public void Open()
        {
            if (Connection != null)
                return;

            if (!string.IsNullOrWhiteSpace(ConnectionString))
                Helper.ParseConnectionString(ConnectionString, Options);

            Connection = MyRawConnectionPool.Next(Options.Key);
            if (Connection != null)
            {
                // Reuse a pooled connection
                Pooled = true;
                Connection.Reset();
            }
            else
            {
                // Create a new connection
                Pooled = false;
                Connection = new Connection();
                HandleOkPacket(Connection.Setup(Options));
            }

            UpdateDatabaseName();
            if (Database != Options.Database)
                ChangeDatabase(Options.Database);
        }

        public void Ping()
        {
            AssertConnected();
            Connection.SendCommand(Commands.Ping);
            HandleOkPacket(Connection.ReadPacket());
        }

        public ResultSet Query(string sql)
        {
            return DoQuery(sql)?.FirstOrDefault();
        }

        public List<ResultSet> QueryMultiple(string sql)
        {
            return DoQuery(sql);
        }

        public object QueryScalar(string sql)
        {
            var result = DoQuery(sql, 1);
            return result == null || result.First().RowCount == 0 ? null : result.First().GetValue(0, 0);
        }

        public T QueryScalar<T>(string sql)
        {
            var result = DoQuery(sql, 1);
            return result == null || result.First().RowCount == 0 ? default(T) : result.First().Get<T>(0, 0);
        }

        public string QuoteIdentifier(string field) => Helper.QuoteIdentifier(field);

        public string QuoteIdentifier(string table, string field) => Helper.QuoteIdentifier(table, field);

        public string QuoteString(string value) => Helper.QuoteString(value);

        public void ResetConnection()
        {
            AssertConnected();
            HandleOkPacket(Connection.Reset());
        }
    }
}
