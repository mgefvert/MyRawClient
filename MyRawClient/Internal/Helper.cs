using System;
using System.Data;
using System.Globalization;
using System.Text;

namespace MyRawClient.Internal
{
    public class Helper
    {
        private static bool IsBackslashChar(char c)
        {
            return c < 0x80
                ? c == 0x005c
                : c == 0x00a5 || c == 0x0160 || c == 0x20a9 || c == 0x2216 || c == 0xfe68 || c == 0xff3c;
        }

        private static bool IsQuoteChar(char c )
        {
            return c < 0x80
                ? c == 0x0022 || c == 0x0027 || c == 0x0060
                : c == 0x00b4 || c == 0x02b9 || c == 0x02ba || c == 0x02bb || c == 0x02bc || c == 0x02c8 ||
                  c == 0x02ca || c == 0x02cb || c == 0x02d9 || c == 0x0300 || c == 0x0301 || c == 0x2018 || 
                  c == 0x2019 || c == 0x201a || c == 0x2032 || c == 0x2035 || c == 0x275b || c == 0x275c || 
                  c == 0xff07;
        }

        private static bool ParseBool(string value)
        {
            if (bool.TryParse(value, out var boolval))
                return boolval;

            if (int.TryParse(value, out var intval))
                return intval != 0;

            throw new MyRawException($"Invalid boolean value '{value}'.");
        }

        public static void ParseConnectionString(string connectionString, Options options)
        {
            var fields = connectionString.Split(';');
            foreach (var field in fields)
            {
                if (string.IsNullOrWhiteSpace(field) || !field.Contains("="))
                    continue;

                var x = field.Split(new[] { '=' }, 2);
                var key = x[0].ToLower().Trim();
                var value = x[1].Trim();

                switch (key)
                {
                    case "compress":
                    case "use compression":
                    case "usecompression":
                        options.UseCompression = ParseBool(value);
                        break;

                    case "connect timeout":
                    case "connection timeout":
                    case "connectiontimeout":
                        options.ConnectTimeout = int.Parse(value);
                        break;

                    case "default command timeout":
                    case "defaultcommandtimeout":
                        options.CommandTimeout = int.Parse(value);
                        break;

                    case "host":
                    case "server":
                    case "data source":
                    case "datasource":
                    case "address":
                    case "addr":
                    case "network address":
                        options.Server = value;
                        break;

                    case "initial catalog":
                    case "database":
                        options.Database = value;
                        break;

                    case "password":
                    case "pwd":
                        options.Password = value;
                        break;

                    case "port":
                        options.Port = ushort.Parse(value);
                        break;

                    case "user id":
                    case "userid":
                    case "username":
                    case "uid":
                    case "user name":
                    case "user":
                        options.User = value;
                        break;

                    default:
                        throw new MyRawException("Connection string statement 'field' is unsupported.");
                }
            }
        }

        public static string PrepareQuery(string query, IDataParameterCollection parameters)
        {
            var sb = new StringBuilder(query.Length);
            var pos = 0;
            var quoteChar = 0;
            while (pos < query.Length)
            {
                var c = query[pos];
                if (quoteChar != 0)
                {
                    // Escape char - just skip one ahead
                    if (c == '\\' && pos < query.Length - 1)
                    {
                        sb.Append(c);
                        c = query[++pos];
                    }
                    else if (c == quoteChar)
                        quoteChar = 0;
                }
                else
                {
                    if (c == '"' || c == '\'')
                    {
                        quoteChar = c;
                    }
                    else if (c == '@')
                    {
                        // This is where we actually want to get to... parse @xyz
                        var identifier = PrepareQueryExtractIdentifier(query, ref pos);
                        sb.Append(parameters.Contains(identifier) ? ValueToSQL(parameters[identifier]) : "@" + identifier);
                        continue;
                    }
                }

                sb.Append(c);
                pos++;
            }

            return sb.ToString();
        }

        private static string PrepareQueryExtractIdentifier(string query, ref int pos)
        {
            var start = ++pos;  // Skip @
            while (pos < query.Length && char.IsLetterOrDigit(query[pos]))
            {
                pos++;
            }

            return query.Substring(start, pos - start);
        }

        public static string QuoteIdentifier(string identifier)
        {
            var sb = new StringBuilder(identifier.Length + 2);
            sb.Append('`');

            foreach (var c in identifier)
            {
                if (!IsBackslashChar(c) && !IsQuoteChar(c))
                    sb.Append(c);
            }

            sb.Append('`');
            return sb.ToString();
        }

        public static string QuoteIdentifier(string table, string field)
        {
            return QuoteIdentifier(table) + "." + QuoteIdentifier(field);
        }

        public static string QuoteString(string value)
        {
            var sb = new StringBuilder(value.Length + 16);
            sb.Append('\'');

            foreach (var c in value)
            {
                if (c == 0)
                {
                    sb.Append("\\0");
                    continue;
                }

                if (IsBackslashChar(c) || IsQuoteChar(c))
                    sb.Append('\\');

                sb.Append(c);
            }

            sb.Append('\'');
            return sb.ToString();
        }

        public static string ValueToSQL(object value)
        {
            if (value == null || value is DBNull)
                return "null";

            if (value is string s)
                return QuoteString(s);

            if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint ||
                value is long || value is ulong)
                return value.ToString();

            if (value is float f)
                return f.ToString(CultureInfo.InvariantCulture);

            if (value is double d)
                return d.ToString(CultureInfo.InvariantCulture);

            if (value is decimal dec)
                return dec.ToString(CultureInfo.InvariantCulture);

            if (value is DateTime dt)
            {
                if (dt.TimeOfDay == TimeSpan.Zero)
                    return "'" + dt.ToString("yyyy-MM-dd") + "'";

                if (dt.Millisecond == 0)
                    return "'" + dt.ToString("yyyy-MM-dd HH:mm:ss") + "'";

                return "'" + dt.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
            }

            if (value is byte[] buffer)
            {
                var result = new StringBuilder(buffer.Length*2 + 3);
                result.Append("x'");
                foreach (var b in buffer)
                    result.Append(b.ToString("X2"));
                result.Append("'");
                return result.ToString();
            }

            return QuoteString(value.ToString());
        }
    }
}
