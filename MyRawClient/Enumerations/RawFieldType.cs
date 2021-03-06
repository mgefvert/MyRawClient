﻿using System;

namespace MyRawClient.Enumerations
{
    public enum RawFieldType : byte
    {
        Decimal = 0x00,
        Tiny = 0x01,
        Short = 0x02,
        Long = 0x03,
        Float = 0x04,
        Double = 0x05,
        Null = 0x06,
        Timestamp = 0x07,
        LongLong = 0x08,
        Int24 = 0x09,
        Date = 0x0a,
        Time = 0x0b,
        DateTime = 0x0c,
        Year = 0x0d,
        NewDate = 0x0e,
        VarChar = 0x0f,
        Bit = 0x10,
        Timestamp2 = 0x11,
        DateTime2 = 0x12,
        Time2 = 0x13,
        NewDecimal = 0xf6,
        Enum = 0xf7,
        Set = 0xf8,
        TinyBlob = 0xf9,
        MediumBlob = 0xfa,
        LongBlob = 0xfb,
        Blob = 0xfc,
        VarString = 0xfd,
        String = 0xfe,
        Geometry = 0xff
    }
}
