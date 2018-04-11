using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MySqlRawDriver
{
    public class MyRawDbParameterCollection : List<MyRawDbParameter>, IDataParameterCollection
    {
        public bool Contains(string parameterName)
        {
            return this.Any(x => x.ParameterName == parameterName);
        }

        public int IndexOf(string parameterName)
        {
            return FindIndex(x => x.ParameterName == parameterName);
        }

        public void RemoveAt(string parameterName)
        {
            RemoveAll(x => x.ParameterName == parameterName);
        }

        public object this[string parameterName]
        {
            get => this.First(x => x.ParameterName == parameterName).Value;
            set => this.First(x => x.ParameterName == parameterName).Value = value;
        }
    }
}
