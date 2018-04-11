using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MySqlRawDriver
{
    public class MyRawDbReader : IDataReader
    {
        public int Depth { get; } = 0;
        public bool IsClosed { get; set; }
        public int RecordsAffected => _result.RowCount;
        public int FieldCount => _result.ColumnCount;
        public object this[int i] => GetValue(i);
        public object this[string name] => GetValue(GetOrdinal(name));

        private MyRawResultSet _result;
        private readonly List<MyRawResultSet> _resultSet;
        private int _row = -1;

        internal MyRawDbReader(List<MyRawResultSet> resultSet)
        {
            _resultSet = resultSet;
            NextResult();
            IsClosed = false;
        }

        public void Close()
        {
            Dispose();
        }

        public bool GetBoolean(int i)
        {
            return _result.GetBool(_row, i);
        }

        public byte GetByte(int i)
        {
            return _result.Get<byte>(_row, i);
        }

        public char GetChar(int i)
        {
            return _result.GetString(_row, i)[0];
        }

        public string GetDataTypeName(int i)
        {
            return _result.Fields[i].DataType.ToString("G");
        }

        public DateTime GetDateTime(int i)
        {
            return _result.Get<DateTime>(_row, i);
        }

        public decimal GetDecimal(int i)
        {
            return _result.Get<decimal>(_row, i);
        }

        public double GetDouble(int i)
        {
            return _result.Get<double>(_row, i);
        }

        public Type GetFieldType(int i)
        {
            return _result.Fields[i].FieldType;
        }

        public float GetFloat(int i)
        {
            return _result.Get<float>(_row, i);
        }

        public Guid GetGuid(int i)
        {
            return Guid.Parse(_result.GetString(_row, i));
        }

        public short GetInt16(int i)
        {
            return _result.Get<short>(_row, i);
        }

        public int GetInt32(int i)
        {
            return _result.Get<int>(_row, i);
        }

        public long GetInt64(int i)
        {
            return _result.Get<long>(_row, i);
        }

        public string GetName(int i)
        {
            return _result.Fields[i].Name;
        }

        public int GetOrdinal(string name)
        {
            return _result.Fields.FindIndex(x => x.Name == name);
        }

        public string GetString(int i)
        {
            return _result.GetString(_row, i);
        }

        public object GetValue(int i)
        {
            return _result.GetValue(_row, i);
        }

        public bool IsDBNull(int i)
        {
            return _result.IsNull(_row, i);
        }

        public bool Read()
        {
            return ++_row < _result.RowCount;
        }

        public long GetBytes(int i, long fieldoffset, byte[] buffer, int bufferoffset, int length)
        {
            return _result.GetBytes(_row, i, fieldoffset, buffer, bufferoffset, length);
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return _result.GetChars(_row, i, fieldoffset, buffer, bufferoffset, length);
        }

        public void Dispose()
        {
            IsClosed = true;
        }

        public int GetValues(object[] values)
        {
            for (var i = 0; i < values.Length; i++)
                values[i] = GetValue(i);

            return values.Length;
        }

        public IDataReader GetData(int i)
        {
            throw new NotImplementedException();
        }
        
        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public bool NextResult()
        {
            _result = _resultSet.FirstOrDefault();
            if (_result == null)
                return false;

            _resultSet.RemoveAt(0);
            return true;
        }
    }
}
