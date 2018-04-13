using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using MySqlRawDriver.Auth;
using MySqlRawDriver.Enumerations;
using MySqlRawDriver.Packets;

namespace MySqlRawDriver
{
    public class MyRawDbConnection : IDbConnection
    {
        public string ConnectionString { get; set; }
        public string Database { get; private set; }
        public MyRawOptions Options { get; } = new MyRawOptions();
        public Result Result { get; protected set; }
        public ConnectionState State { get; protected set; } = ConnectionState.Closed;
        public ServerInfo ServerInfo { get; } = new ServerInfo();

        public int ConnectionTimeout => Options.ConnectTimeout;
        public long LastInsertId => Result?.LastInsertId ?? 0;
        public long RowsAffected => Result?.RowsAffected ?? 0;

        private Stream _stream;
        private TcpClient _client;
        private byte _sequence;
        private readonly byte[] _lastHeader = new byte[4];

        public MyRawDbConnection()
        {
        }

        public MyRawDbConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }

        ~MyRawDbConnection()
        {
            Dispose();
        }

        public void Dispose()
        {
            Close();
        }


        // --- Internal methods ---

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        protected void AssertStateIs(params ConnectionState[] states)
        {
            if (!states.Contains(State))
                throw new InvalidOperationException("Unable to perform operation when connection state is " + State);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        protected void AssertStateIsNot(params ConnectionState[] states)
        {
            if (states.Contains(State))
                throw new InvalidOperationException("Unable to perform operation when connection state is " + State);
        }

        protected List<MyRawResultSet> DoQuery(string sql, int capacity = 256)
        {
            AssertStateIs(ConnectionState.Open);

            _sequence = 0;
            State = ConnectionState.Executing;

            // Build and send query
            var builder = new PacketBuilder(Options.Encoding, _sequence++);
            builder.AppendInt1((byte)Commands.Query);
            builder.AppendStringFixed(sql);
            SendPacket(builder.ToPacket());

            List<MyRawResultSet> results = null;
            do
            {
                // Read response
                var packet = ReadPacket();

                // Execute queries with no result set
                if (PacketReader.IsOkPacket(packet))
                {
                    HandleOkPacket(packet);
                    State = ConnectionState.Open;
                    return results;
                }

                // Read column count and initialize reader
                var position = 0;
                var columnCount = (int) PacketReader.ReadIntLengthEncoded(packet, ref position);
                var resultSet = new MyRawResultSet(columnCount, capacity, Options.Encoding);

                // Fetch column definitions
                State = ConnectionState.Fetching;
                for (var i = 0; i < columnCount; i++)
                    resultSet.Fields.Add(FetchColumnDefinition());

                HandleOkPacket(ReadPacket());

                // Fetch all data
                for (;;)
                {
                    packet = ReadPacket();
                    if (PacketReader.IsOkPacket(packet))
                    {
                        HandleOkPacket(packet);
                        break;
                    }

                    resultSet.AddBuffer(packet);
                }

                if (results == null)
                    results = new List<MyRawResultSet>();
                results.Add(resultSet);
            } while (Result.Status.HasFlag(StatusFlags.MoreResultsExist));

            State = ConnectionState.Open;
            return results;
        }

        private MyRawResultField FetchColumnDefinition()
        {
            var item = new MyRawResultField();
            var packet = ReadPacket();
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

            item.InternalType = MyRawResultField.DataTypeToInternalType(item.DataType, item.Flags);
            item.FieldType = MyRawResultField.InternalTypeToType(item.InternalType);

            return item;
        }

        protected void HandleOkPacket(byte[] packet)
        {
            Result = Result.Decode(packet, Options.Encoding);
        }

        protected void InitializeConnection()
        {
            var packet = ReadPacket();
            var handshake = HandshakeRequest.Decode(packet, Options.Encoding);
            ServerInfo.Capabilities = handshake.Capabilities;
            ServerInfo.CharacterSet = handshake.CharacterSet;
            ServerInfo.ConnectionId = handshake.ConnectionId;
            ServerInfo.ServerVersion = handshake.ServerVersion;

            SendPacket(new HandshakeResponse
            {
                Capabilities = CapabilityFlags.ClientLongPassword | CapabilityFlags.ClientConnectWithDB | CapabilityFlags.ClientSecureConnection |
                               CapabilityFlags.ClientProtocol41 | CapabilityFlags.ClientMultiStatements | CapabilityFlags.ClientMultiResults,
                MaxPacketSize = 65536,
                CharacterSet = 33,
                User = Options.User,
                Database = Options.Database,
                Password = NativePassword.Encrypt(Options.Encoding.GetBytes(Options.Password), handshake.AuthData)
            }.Encode(Options.Encoding, _sequence++));

            var response = ReadPacket();
            if (PacketReader.IsOkPacket(response))
                HandleOkPacket(response);
            else
                throw new MyRawException("Unrecognized packet in handshake: " + PacketReader.PacketToString(response));
        }

        protected byte[] ReadPacket()
        {
            try
            {
                if (_stream.Read(_lastHeader, 0, 4) != 4)
                    throw new MyRawException("Invalid data received from server, closing connection.");

                var sequence = _lastHeader[3];
                if (sequence != _sequence++)
                    throw new MyRawException("Sequence ID out of sync, closing connection.");

                _lastHeader[3] = 0;
                var pktlen = BitConverter.ToInt32(_lastHeader, 0);
                _lastHeader[3] = sequence;

                var buffer = new byte[pktlen];
                if (_stream.Read(buffer, 0, pktlen) != pktlen)
                    throw new MyRawException("Invalid data received from server, closing connection.");

                if (PacketReader.IsErrPacket(buffer))
                    throw MyRawException.Decode(buffer, Options.Encoding);

                return buffer;
            }
            catch (Exception)
            {
                Close();
                throw;
            }
        }


        protected void SendPacket(byte[] buffer)
        {
            _stream.Write(buffer, 0, buffer.Length);
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
            return new MyRawDbTransaction(this);
        }

        public void Close()
        {
            if (_stream != null)
            {
                try
                {
                    _sequence = 0;
                    var builder = new PacketBuilder(Options.Encoding, _sequence++);
                    builder.AppendInt1((byte)Commands.Quit);
                    SendPacket(builder.ToPacket());
                }
                catch
                {
                    //
                }
            }

            try
            {
                _stream = null;
                _client?.Close();
                _client = null;
            }
            catch
            {
                //
            }

            State = ConnectionState.Closed;
        }

        public void ChangeDatabase(string databaseName)
        {
            Execute("use " + QuoteIdentifier(databaseName));
            UpdateDatabaseName();
        }

        public IDbCommand CreateCommand()
        {
            return new MyRawDbCommand(this);
        }

        public Result Execute(string sql)
        {
            DoQuery(sql);
            return Result;
        }

        public void Open()
        {
            if (!string.IsNullOrWhiteSpace(ConnectionString))
                MyRawHelper.ParseConnectionString(ConnectionString, Options);

            AssertStateIs(ConnectionState.Closed);
            State = ConnectionState.Connecting;
            try
            {
                _client = new TcpClient
                {
                    ReceiveTimeout = Options.ConnectTimeout * 1000,
                    ReceiveBufferSize = 32768
                };
                _client.Connect(Options.Server, Options.Port);
                _stream = new BufferedStream(_client.GetStream(), 16384);

                try
                {
                    InitializeConnection();
                    State = ConnectionState.Open;
                    _client.ReceiveTimeout = Options.CommandTimeout * 1000;
                    _client.SendTimeout = Options.CommandTimeout * 1000;

                    UpdateDatabaseName();
                }
                catch (Exception)
                {
                    Close();
                    throw;
                }
            }
            catch (Exception)
            {
                State = ConnectionState.Closed;
                throw;
            }
        }

        public void Ping()
        {
            AssertStateIs(ConnectionState.Open);

            _sequence = 0;
            var builder = new PacketBuilder(Options.Encoding, _sequence++);
            builder.AppendInt1((byte)Commands.Ping);

            SendPacket(builder.ToPacket());
            HandleOkPacket(ReadPacket());
        }

        public MyRawResultSet Query(string sql)
        {
            return DoQuery(sql)?.FirstOrDefault();
        }

        public List<MyRawResultSet> QueryMultiple(string sql)
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

        public string QuoteIdentifier(string field) => MyRawHelper.QuoteIdentifier(field);

        public string QuoteIdentifier(string table, string field) => MyRawHelper.QuoteIdentifier(table, field);

        public string QuoteString(string value) => MyRawHelper.QuoteString(value);

        public void ResetConnection()
        {
            AssertStateIs(ConnectionState.Open);

            _sequence = 0;
            var builder = new PacketBuilder(Options.Encoding, _sequence++);
            builder.AppendInt1((byte)Commands.ResetConnection);

            SendPacket(builder.ToPacket());
            HandleOkPacket(ReadPacket());
            State = ConnectionState.Open;
        }
    }
}
