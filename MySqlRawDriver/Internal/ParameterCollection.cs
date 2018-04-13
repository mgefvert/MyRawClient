using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MySqlRawDriver.Internal
{
    public class ParameterCollection : List<Parameter>, IDataParameterCollection
    {
        private bool Like(string a, string b) => string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase) == 0;

        public bool Contains(string parameterName)
        {
            return this.Any(x => Like(x.ParameterName, parameterName));
        }

        public int IndexOf(string parameterName)
        {
            return FindIndex(x => Like(x.ParameterName, parameterName));
        }

        public void RemoveAt(string parameterName)
        {
            RemoveAll(x => Like(x.ParameterName, parameterName));
        }

        public object this[string parameterName]
        {
            get => this.First(x => Like(x.ParameterName, parameterName)).Value;
            set => this.First(x => Like(x.ParameterName, parameterName)).Value = value;
        }
    }
}
