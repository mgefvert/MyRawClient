using System;

namespace MySqlRawDriver.Enumerations
{
    public enum ResultMode : byte
    {
        Ok = 0,
        Eof = 0xFE,
        Error = 0xFF
    }
}
