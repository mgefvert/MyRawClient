using System;
using MySqlRawDriver.Enumerations;

namespace MySqlRawDriver.Internal
{
    public class ResultField
    {
        public string Catalog { get; set; }
        public ushort CharSet { get; set; }
        public int Decimals { get; set; }
        public int FieldLength { get; set; }
        public RawFieldType DataType { get; set; }
        public ColumnFlags Flags { get; set; }
        public string Name { get; set; }
        public string OrgName { get; set; }
        public string OrgTable { get; set; }
        public string Schema { get; set; }
        public string Table { get; set; }
        public InternalType InternalType { get; set; }
        public Type FieldType { get; set; }

        public bool IsAutoIncrement => Flags.HasFlag(ColumnFlags.AutoIncrement);
        public bool IsBlob => Flags.HasFlag(ColumnFlags.Blob);
        public bool IsEnum => Flags.HasFlag(ColumnFlags.Enum);
        public bool IsNullable => !Flags.HasFlag(ColumnFlags.NotNull);
        public bool IsPrimaryKey => Flags.HasFlag(ColumnFlags.PrimaryKey);
        public bool IsUniqueKey => Flags.HasFlag(ColumnFlags.UniqueKey);
        public bool IsUnsigned => Flags.HasFlag(ColumnFlags.Unsigned);

        public static InternalType DataTypeToInternalType(RawFieldType type, ColumnFlags flags)
        {
            switch (type)
            {
                case RawFieldType.Decimal:
                case RawFieldType.NewDecimal:
                    return InternalType.Decimal;

                case RawFieldType.Bit:
                case RawFieldType.Tiny:
                    return flags.HasFlag(ColumnFlags.Unsigned) ? InternalType.UTiny : InternalType.Tiny;

                case RawFieldType.Short:
                    return flags.HasFlag(ColumnFlags.Unsigned) ? InternalType.UShort : InternalType.Short;

                case RawFieldType.Int24:
                case RawFieldType.Long:
                case RawFieldType.Year:
                    return flags.HasFlag(ColumnFlags.Unsigned) ? InternalType.UInt : InternalType.Int;

                case RawFieldType.Float:
                    return InternalType.Float;

                case RawFieldType.Double:
                    return InternalType.Double;

                case RawFieldType.LongLong:
                    return flags.HasFlag(ColumnFlags.Unsigned) ? InternalType.ULong : InternalType.Long;

                case RawFieldType.Date:
                case RawFieldType.DateTime:
                case RawFieldType.DateTime2:
                case RawFieldType.NewDate:
                case RawFieldType.Time:
                case RawFieldType.Time2:
                case RawFieldType.Timestamp:
                case RawFieldType.Timestamp2:
                    return InternalType.DateTime;

                case RawFieldType.Enum:
                case RawFieldType.Null:
                case RawFieldType.Set:
                case RawFieldType.String:
                case RawFieldType.VarChar:
                case RawFieldType.VarString:
                    return InternalType.String;

                case RawFieldType.Blob:
                case RawFieldType.Geometry:
                case RawFieldType.LongBlob:
                case RawFieldType.MediumBlob:
                case RawFieldType.TinyBlob:
                    return InternalType.ByteArray;

                default:
                    return InternalType.String;
            }
        }

        public static Type InternalTypeToType(InternalType type)
        {
            switch (type)
            {
                case InternalType.String:
                    return typeof(string);
                case InternalType.Decimal:
                    return typeof(decimal);
                case InternalType.Tiny:
                    return typeof(sbyte);
                case InternalType.UTiny:
                    return typeof(byte);
                case InternalType.Short:
                    return typeof(short);
                case InternalType.UShort:
                    return typeof(ushort);
                case InternalType.Int:
                    return typeof(int);
                case InternalType.UInt:
                    return typeof(uint);
                case InternalType.Float:
                    return typeof(float);
                case InternalType.Double:
                    return typeof(double);
                case InternalType.Long:
                    return typeof(long);
                case InternalType.ULong:
                    return typeof(ulong);
                case InternalType.DateTime:
                    return typeof(DateTime);
                case InternalType.ByteArray:
                    return typeof(byte[]);
                default:
                    return typeof(string);
            }
        }
    }
}
