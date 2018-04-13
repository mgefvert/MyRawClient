using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySqlRawDriver.Internal;

namespace MySqlRawDriver
{
    public class MyRawReader : IDataReader
    {
        private ResultSet _current;
        private readonly List<ResultSet> _list;
        private int _row = -1;

        private static readonly Tuple<string, Type, bool>[] SchemaTableDefinition =
        {
            new Tuple<string, Type, bool>("ColumnName", typeof(string), false),
            new Tuple<string, Type, bool>("ColumnOrdinal", typeof(int), false),
            new Tuple<string, Type, bool>("ColumnSize", typeof(int), false),
            new Tuple<string, Type, bool>("NumericPrecision", typeof(int), false),
            new Tuple<string, Type, bool>("NumericScale", typeof(int), false),
            new Tuple<string, Type, bool>("IsUnique", typeof(bool), false),
            new Tuple<string, Type, bool>("IsKey", typeof(bool), true),
            new Tuple<string, Type, bool>("BaseCatalogName", typeof(string), false),
            new Tuple<string, Type, bool>("BaseColumnName", typeof(string), false),
            new Tuple<string, Type, bool>("BaseSchemaName", typeof(string), false),
            new Tuple<string, Type, bool>("BaseTableName", typeof(string), false),
            new Tuple<string, Type, bool>("DataType", typeof(Type), false),
            new Tuple<string, Type, bool>("AllowDBNull", typeof(bool), false),
            new Tuple<string, Type, bool>("ProviderType", typeof(int), false),
            new Tuple<string, Type, bool>("IsAliased", typeof(bool), false),
            new Tuple<string, Type, bool>("IsExpression", typeof(bool), false),
            new Tuple<string, Type, bool>("IsIdentity", typeof(bool), false),
            new Tuple<string, Type, bool>("IsAutoIncrement", typeof(bool), false),
            new Tuple<string, Type, bool>("IsRowVersion", typeof(bool), false),
            new Tuple<string, Type, bool>("IsHidden", typeof(bool), false),
            new Tuple<string, Type, bool>("IsLong", typeof(bool), false),
            new Tuple<string, Type, bool>("IsReadOnly", typeof(bool), false)
        };

        public ResultSet CurrentResult => _current ?? throw new DataException("No active result set.");

        public int Depth { get; } = 0;
        public bool IsClosed { get; set; }
        public int RecordsAffected => CurrentResult.RowCount;
        public int FieldCount => CurrentResult.FieldCount;
        public object this[int i] => GetValue(i);
        public object this[string name] => GetValue(GetOrdinal(name));

        internal MyRawReader(List<ResultSet> list)
        {
            _list = list;
            IsClosed = false;
            NextResult();
        }

        public void Dispose()
        {
            IsClosed = true;
        }

        public void Close()
        {
            Dispose();
        }

        public bool GetBoolean(int i)
        {
            return CurrentResult.GetBool(_row, i);
        }

        public byte GetByte(int i)
        {
            return CurrentResult.Get<byte>(_row, i);
        }

        public long GetBytes(int i, long fieldoffset, byte[] buffer, int bufferoffset, int length)
        {
            return CurrentResult.GetBytes(_row, i, fieldoffset, buffer, bufferoffset, length);
        }

        public char GetChar(int i)
        {
            return CurrentResult.GetString(_row, i)[0];
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            return CurrentResult.GetChars(_row, i, fieldoffset, buffer, bufferoffset, length);
        }

        public IDataReader GetData(int i)
        {
            return null;
        }

        public string GetDataTypeName(int i)
        {
            return CurrentResult.Fields[i].DataType.ToString("G");
        }

        public DateTime GetDateTime(int i)
        {
            return CurrentResult.Get<DateTime>(_row, i);
        }

        public decimal GetDecimal(int i)
        {
            return CurrentResult.Get<decimal>(_row, i);
        }

        public double GetDouble(int i)
        {
            return CurrentResult.Get<double>(_row, i);
        }

        public Type GetFieldType(int i)
        {
            return CurrentResult.Fields[i].FieldType;
        }

        public float GetFloat(int i)
        {
            return CurrentResult.Get<float>(_row, i);
        }

        public Guid GetGuid(int i)
        {
            return Guid.Parse(CurrentResult.GetString(_row, i));
        }

        public short GetInt16(int i)
        {
            return CurrentResult.Get<short>(_row, i);
        }

        public int GetInt32(int i)
        {
            return CurrentResult.Get<int>(_row, i);
        }

        public long GetInt64(int i)
        {
            return CurrentResult.Get<long>(_row, i);
        }

        public string GetName(int i)
        {
            return CurrentResult.Fields[i].Name;
        }

        public int GetOrdinal(string name)
        {
            return CurrentResult.Fields.FindIndex(x => x.Name == name);
        }

        public DataTable GetSchemaTable()
        {
            var result = new DataTable("SchemaTable");
            result.Columns.AddRange(SchemaTableDefinition.Select(x => new DataColumn(x.Item1, x.Item2) { AllowDBNull = x.Item3 }).ToArray());

            var pos = 1;
            foreach (var field in CurrentResult.Fields)
            {
                result.Rows.Add(
                    field.Name,            // ColumnName
                    pos++,                 // ColumnOrdinal
                    field.FieldLength,     // ColumnSize
                    field.Decimals,        // NumericPrecision
                    0,                     // NumericScale ???
                    false,                 // IsUnique
                    field.IsPrimaryKey,    // IsKey
                    null,                  // BaseCatalogName
                    field.OrgName,         // BaseColumnName
                    field.Schema,          // BaseSchemaName
                    field.Table,           // BaseTableName
                    field.FieldType,       // DataType
                    field.IsNullable,      // AllowDBNull
                    (int)field.DataType,   // ProviderType
                    false,                 // IsAliased
                    false,                 // IsExpression
                    false,                 // IsIdentity
                    field.IsAutoIncrement, // IsAutoIncrement
                    false,                 // IsRowVersion
                    false,                 // IsHidden
                    false,                 // IsLong
                    false                  // IsReadOnly
                );
            }

            return result;
        }

        public string GetString(int i)
        {
            return CurrentResult.GetString(_row, i);
        }

        public object GetValue(int i)
        {
            return CurrentResult.GetValue(_row, i);
        }

        public int GetValues(object[] values)
        {
            for (var i = 0; i < values.Length; i++)
                values[i] = GetValue(i);

            return values.Length;
        }

        public bool IsDBNull(int i)
        {
            return CurrentResult.IsNull(_row, i);
        }

        public bool NextResult()
        {
            _row = -1;
            _current = _list.FirstOrDefault();
            if (_current == null)
                return false;

            _list.RemoveAt(0);
            return true;
        }

        public bool Read()
        {
            return ++_row < CurrentResult.RowCount;
        }
    }
}
