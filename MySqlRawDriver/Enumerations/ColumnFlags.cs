using System;

namespace MySqlRawDriver.Enumerations
{
    [Flags]
    public enum ColumnFlags : ushort
    {
        NotNull = 1,
        PrimaryKey = 2,
        UniqueKey = 4,
        MultipleKey = 8,
        Blob = 0x10,
        Unsigned = 0x20,
        ZeroFill = 0x40,
        Binary = 0x80,
        Enum = 0x100,
        AutoIncrement = 0x200,
        Timestamp = 0x400,
        Set = 0x800,
        Number = 0x8000
    }
}
