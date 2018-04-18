using System;
using System.IO;

namespace MyRawClient.PacketHandlers
{
    public interface IPacketHandler
    {
        byte[] ReadPacket(Stream stream);
        void SendPacket(Stream stream, byte[] buffer, bool newCommand);
    }
}
