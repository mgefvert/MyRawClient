using System;

namespace MyRawClient.PacketHandler
{
    public interface IPacketHandler
    {
        byte[] ReadPacket();
        void SendPacket(byte[] buffer, bool newCommand);
    }
}
