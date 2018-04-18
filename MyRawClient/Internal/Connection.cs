using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using MyRawClient.Auth;
using MyRawClient.Enumerations;
using MyRawClient.PacketHandlers;
using MyRawClient.Packets;

namespace MyRawClient.Internal
{
    internal class Connection
    {
        private static readonly CapabilityFlags DefaultCapabilities =
            CapabilityFlags.ClientLongPassword |
            CapabilityFlags.ClientConnectWithDB |
            CapabilityFlags.ClientSecureConnection |
            CapabilityFlags.ClientProtocol41 |
            CapabilityFlags.ClientMultiStatements |
            CapabilityFlags.ClientMultiResults;

        public TcpClient Client { get; private set; }
        public IPacketHandler Handler { get; private set; }
        public ServerInfo ServerInfo { get; } = new ServerInfo();
        public ConnectionState State { get; internal set; } = ConnectionState.Closed;
        public Stream Stream { get; private set; }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        internal void AssertStateIs(params ConnectionState[] states)
        {
            if (!states.Contains(State))
                throw new InvalidOperationException("Unable to perform operation when connection state is " + State);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        internal void AssertStateIsNot(params ConnectionState[] states)
        {
            if (states.Contains(State))
                throw new InvalidOperationException("Unable to perform operation when connection state is " + State);
        }

        private byte[] InitializeConnection(Options options)
        {
            var packet = ReadPacket();
            var handshake = HandshakeRequest.Decode(packet, options.Encoding);
            ServerInfo.Capabilities = handshake.Capabilities;
            ServerInfo.CharacterSet = handshake.CharacterSet;
            ServerInfo.ConnectionId = handshake.ConnectionId;
            ServerInfo.ServerVersion = handshake.ServerVersion;

            var mycaps = DefaultCapabilities;
            if (options.UseCompression)
                mycaps |= CapabilityFlags.ClientCompress;

            SendPacket(new HandshakeResponse
            {
                Capabilities = mycaps,
                MaxPacketSize = 65536,
                CharacterSet = 33,
                User = options.User,
                Database = options.Database,
                Password = NativePassword.Encrypt(options.Encoding.GetBytes(options.Password), handshake.AuthData)
            }.Encode(options.Encoding), false);

            var response = ReadPacket();
            if (!PacketReader.IsOkPacket(response))
                throw new MyRawException("Unrecognized packet in handshake: " + PacketReader.PacketToString(response));

            if (ServerInfo.Capabilities.HasFlag(CapabilityFlags.ClientCompress) && options.UseCompression)
                Handler = new CompressedPacketHandler(Handler);

            return response;
        }

        public byte[] ReadPacket()
        {
            return Handler.ReadPacket(Stream);
        }

        public byte[] Reset()
        {
            AssertStateIs(ConnectionState.Open);

            SendCommand(Commands.ResetConnection);
            var result = ReadPacket();
            State = ConnectionState.Open;

            return result;
        }

        public void SendCommand(Commands command)
        {
            AssertStateIs(ConnectionState.Open);
            Handler.SendPacket(Stream, PacketBuilder.MakeCommand(command), true);
        }

        public void SendPacket(byte[] packet, bool newCommand)
        {
            Handler.SendPacket(Stream, packet, newCommand);
        }

        public byte[] Setup(Options options)
        {
            State = ConnectionState.Connecting;
            try
            {
                Client = new TcpClient
                {
                    ReceiveTimeout = options.ConnectTimeout * 1000,
                    ReceiveBufferSize = 32768
                };
                Client.Connect(options.Server, options.Port);
                Stream = new BufferedStream(Client.GetStream(), 16384);
                Handler = new DefaultPacketHandler(options.Encoding);

                try
                {
                    var result = InitializeConnection(options);
                    State = ConnectionState.Open;
                    Client.ReceiveTimeout = options.CommandTimeout * 1000;
                    Client.SendTimeout = options.CommandTimeout * 1000;

                    return result;
                }
                catch (Exception)
                {
                    Shutdown();
                    throw;
                }
            }
            catch (Exception)
            {
                State = ConnectionState.Closed;
                throw;
            }
        }

        public void Shutdown()
        {
            if (Stream != null)
            {
                try { SendPacket(PacketBuilder.MakeCommand(Commands.Quit), true); }
                catch { /**/ }
            }

            try
            {
                Stream = null;
                Client?.Close();
                Client = null;
            }
            catch { /**/ }

            State = ConnectionState.Closed;
        }
    }
}
