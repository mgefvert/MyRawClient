using System;

namespace MyRawClient.Enumerations
{
    [Flags]
    public enum StatusFlags : ushort
    {
        InTransaction = 1,
        AutoCommit = 2,
        MoreResultsExist = 8,
        NoGoodIndexUsed = 0x10,
        NoIndexUsed = 0x20,
        CursorExists = 0x40,
        LastRowSent = 0x80,
        DatabaseDropped = 0x100,
        NoBackslashEscapes = 0x200,
        MetadataChanged = 0x400,
        QueryWasSlow = 0x800,
        PsOutParams = 0x1000,
        InReadonlyTransaction = 0x2000,
        SessionStateChanged = 0x4000
    }
}
