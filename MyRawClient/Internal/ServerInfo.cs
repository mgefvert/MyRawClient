using System;
using MyRawClient.Enumerations;

namespace MyRawClient.Internal
{
    public class ServerInfo
    {
        public CapabilityFlags Capabilities { get; internal set; }
        public byte CharacterSet { get; internal set; }
        public uint ConnectionId { get; internal set; }
        public string ServerVersion { get; internal set; }
    }
}
