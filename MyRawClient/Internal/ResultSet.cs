using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using MyRawClient.Enumerations;
using MyRawClient.Packets;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace MyRawClient.Internal
{
    public class ResultSet : IDisposable
    {
        private readonly Encoding _encoding;
        private readonly List<byte[]> _buffers;
        private readonly List<int> _lengths;
        private readonly List<int> _offsets;
        private readonly int _fields;
        private int _rows;

        public List<ResultField> Fields { get; } = new List<ResultField>();
        public int RowCount => _rows;
        public int FieldCount => _fields;

        public ResultSet(int fields, int capacity, Encoding encoding)
        {
            _fields = fields;
            _buffers = new List<byte[]>(capacity);
            _lengths = new List<int>(capacity * fields);
            _offsets = new List<int>(capacity * fields);
            _encoding = encoding;
        }

        public void Dispose()
        {
            _buffers.Clear();
            _rows = -1;
        }

        internal void AddBuffer(byte[] buffer)
        {
            CheckDisposed();
            _buffers.Add(buffer);
            
            var position = 0;
            for (var i = 0; i < _fields; i++)
            {
                var isNull = PacketReader.ConsumeNull(buffer, ref position);
                if (isNull)
                {
                    _offsets.Add(-1);
                    _lengths.Add(0);
                }
                else
                {
                    var len = (int) PacketReader.ReadIntLengthEncoded(buffer, ref position);
                    _offsets.Add(position);
                    _lengths.Add(len);
                    position += len;
                }
            }

            _rows++;
        }

        private void CheckDisposed()
        {
            if (_rows == -1)
                throw new ObjectDisposedException("Result set is disposed.");
        }

        public T Get<T>(int row, int column)
        {
            var s = GetValue(row, column);
            if (s == null)
                return default(T);

            return s.GetType() == typeof(T) ? (T)s : (T)Convert.ChangeType(s, typeof(T), CultureInfo.InvariantCulture);
        }

        public bool GetBool(int row, int column)
        {
            var s = GetString(row, column).ToLower();
            if (int.TryParse(s, out var result))
                return result != 0;

            if (s == "y" || s == "yes" || s == "t" || s == "true")
                return true;

            if (s == "n" || s == "no" || s == "f" || s == "false")
                return false;

            return bool.Parse(s);
        }

        public byte[] GetBytes(int row, int column)
        {
            GetReference(row, column, out var rbuffer, out var rofs, out var rlen);

            var result = new byte[rlen];
            Array.Copy(rbuffer, rofs, result, 0, rlen);
            return result;
        }

        public long GetBytes(int row, int column, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            GetReference(row, column, out var rbuffer, out var rofs, out var rlen);

            if (fieldOffset >= rlen)
                return 0;
            if (fieldOffset != 0)
            {
                rofs += (int)fieldOffset;
                rlen -= (int)fieldOffset;
                if (length > rlen)
                    length = rlen;
            }

            Array.Copy(rbuffer, rofs, buffer, bufferOffset, length);
            return length;
        }

        public long GetChars(int row, int column, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            var s = GetString(row, column).Substring((int)fieldOffset, length);
            foreach (var c in s)
                buffer[bufferOffset++] = c;

            return s.Length;
        }

        public void GetReference(int row, int column, out byte[] buffer, out int offset, out int length)
        {
            var index = MakeIndex(row, column);
            buffer = _buffers[row];
            offset = _offsets[index];
            length = _lengths[index];
        }

        public string GetString(int row, int column)
        {
            GetReference(row, column, out var buffer, out var offset, out var length);
            return offset == -1 ? null : _encoding.GetString(buffer, offset, length);
        }

        public object GetValue(int row, int column)
        {
            if (IsNull(row, column))
                return null;

            var field = Fields[column];
            switch (field.InternalType)
            {
                case InternalType.String:
                    return GetString(row, column);
                case InternalType.Decimal:
                    return decimal.Parse(GetString(row, column), CultureInfo.InvariantCulture);
                case InternalType.Tiny:
                    return (sbyte)ParseSigned(row, column, "sbyte", sbyte.MinValue, sbyte.MaxValue);
                case InternalType.UTiny:
                    return (byte)ParseUnsigned(row, column, "sbyte", byte.MaxValue);
                case InternalType.Short:
                    return (short)ParseSigned(row, column, "short", short.MinValue, short.MaxValue);
                case InternalType.UShort:
                    return (ushort)ParseUnsigned(row, column, "ushort", ushort.MaxValue);
                case InternalType.Int:
                    return (int)ParseSigned(row, column, "int", int.MinValue, int.MaxValue);
                case InternalType.UInt:
                    return (uint)ParseUnsigned(row, column, "uint", uint.MaxValue);
                case InternalType.Long:
                    return ParseSigned(row, column, "long", long.MinValue, long.MaxValue);
                case InternalType.ULong:
                    return ParseUnsigned(row, column, "ulong", ulong.MaxValue);
                case InternalType.Float:
                    return float.Parse(GetString(row, column), CultureInfo.InvariantCulture);
                case InternalType.Double:
                    return double.Parse(GetString(row, column), CultureInfo.InvariantCulture);
                case InternalType.DateTime:
                    return DateTime.Parse(GetString(row, column), CultureInfo.InvariantCulture);
                case InternalType.ByteArray:
                    return GetBytes(row, column);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Exception InvalidData(int row, int column, string desiredType)
        {
            return new InvalidCastException("Unable to cast <" + (GetString(row, column) ?? "(null)") + "> to " + desiredType);
        }

        public bool IsNull(int row, int column)
        {
            return _offsets[MakeIndex(row, column)] == -1;
        }

        private int MakeIndex(int row, int column)
        {
            CheckDisposed();
            return row < _rows && column < _fields 
                ? row * _fields + column 
                : throw new MyRawException($"Row {row}, column {column} does not exist");
        }

        private void ParseNumber(int row, int column, string desiredType, out ulong number, out bool sign)
        {
            number = 0;

            GetReference(row, column, out var buffer, out var offset, out var length);
            if (offset == -1 || length <= 0)
                throw InvalidData(row, column, desiredType);

            sign = buffer[offset] == '-';
            if (sign)
            {
                offset++;
                length--;
            }

            while (length > 0)
            {
                var c = buffer[offset] - 48;
                if (c < 0 || c > 10)
                    throw InvalidData(row, column, desiredType);

                number = number * 10 + (byte)c;
                length--;
                offset++;
            }
        }

        private ulong ParseUnsigned(int row, int column, string desiredType, ulong max)
        {
            ParseNumber(row, column, desiredType, out var number, out var sign);

            if (number > max || sign)
                throw InvalidData(row, column, desiredType);

            return number;
        }

        private long ParseSigned(int row, int column, string desiredType, long min, long max)
        {
            ParseNumber(row, column, desiredType, out var number, out var sign);

            if (number > (ulong)max)
                throw InvalidData(row, column, desiredType);

            if (sign && number > (ulong) -min)
                throw InvalidData(row, column, desiredType);

            return sign ? -(long)number : (long)number;
        }
    }
}
