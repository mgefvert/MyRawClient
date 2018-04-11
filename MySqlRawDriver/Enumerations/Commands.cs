using System;

namespace MySqlRawDriver.Enumerations
{
    public enum Commands : byte
    {
        Quit = 0x01,
        Query = 0x03,
        Ping = 0x0E,
        ResetConnection = 0x1F
    }
}
