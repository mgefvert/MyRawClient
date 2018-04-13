using System;
using System.Text;

namespace MySqlRawDriver.Internal
{
    public class Options
    {
        public int CommandTimeout { get; set; } = 30;
        public int ConnectTimeout { get; set; } = 15;
        public string Database { get; set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string Password { get; set; }
        public ushort Port { get; set; } = 3306;
        public string Server { get; set; }
        public string User { get; set; }
    }
}
