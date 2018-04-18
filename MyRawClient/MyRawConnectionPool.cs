using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MyRawClient.Enumerations;
using MyRawClient.Internal;
using MyRawClient.Packets;

namespace MyRawClient
{
    public static class MyRawConnectionPool
    {
        private const int CheckConnectionInterval = 60;
        private const int CloseConnectionInterval = 60 * 15;

        private class ConnectionPoolItem
        {
            public Connection Connection { get; }
            public DateTime Origin { get; }
            public DateTime Time { get; }

            public ConnectionPoolItem(Connection connection, DateTime? originalTime = null)
            {
                Connection = connection;
                Time = DateTime.Now;
                Origin = originalTime ?? DateTime.Now;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private static readonly Timer Timer = new Timer(state => CheckConnections(), null, 15000, 15000);
        private static readonly object Lock = new object();
        private static readonly Dictionary<string, List<ConnectionPoolItem>> Items = new Dictionary<string, List<ConnectionPoolItem>>();
        
        internal static bool Debug { get; set; }
        public static bool Enabled { get; set; } = true;

        public static int ActiveCount
        {
            get
            {
                lock (Lock)
                {
                    return Items.Count;
                }
            }
        }
        
        private static void CheckConnections()
        {
            if (Debug) WriteDebug("#pool.check: checking connections");

            string[] keys;
            lock (Lock)
            {
                keys = Items.Keys.ToArray();
            }

            foreach (var key in keys)
                CheckConnections(key);

            lock (Lock)
            {
                foreach (var key in Items.Keys.ToArray())
                    if (Items[key].Count == 0)
                        Items.Remove(key);
            }

            if (Debug) WriteDebug("#pool.check: all done");
        }

        private static void CheckConnections(string key)
        {
            List<ConnectionPoolItem> items;
            lock (Lock)
            {
                var list = Items[key];

                items = list.Where(x => (DateTime.Now - x.Time).TotalSeconds > CheckConnectionInterval).ToList();
                foreach (var item in items)
                    list.Remove(item);

                if (Debug) WriteDebug($"#pool.check({key}): checking {items.Count} items, {list.Count} remaining untouched in list");
            }

            foreach (var item in items)
            {
                if (!Enabled || (DateTime.Now - item.Origin).TotalSeconds > CloseConnectionInterval)
                    Discard(item);
                else
                    Consume(key, item.Connection, item.Origin);
            }
        }

        internal static void Consume(string key, Connection connection, DateTime? originalTime)
        {
            if (connection == null)
                return;

            if (!Enabled)
            {
                if (Debug) WriteDebug("#pool.consume: not enabled, discarding " + connection.ServerInfo.ConnectionId);
                connection.Shutdown();
                return;
            }

            var item = new ConnectionPoolItem(connection, originalTime);
            if (!TestConnection(item))
            {
                Discard(item);
                return;
            }

            lock (Lock)
            {
                if (!Items.ContainsKey(key))
                    Items[key] = new List<ConnectionPoolItem>();

                Items[key].Add(item);
                if (Debug) WriteDebug("#pool.consume: added connection " + connection.ServerInfo.ConnectionId);
            }
        }

        private static void Discard(ConnectionPoolItem item)
        {
            if (Debug) WriteDebug("#pool.discard: closing connection " + item.Connection.ServerInfo.ConnectionId);
            item.Connection.Shutdown();
        }

        private static ConnectionPoolItem GetNext(string key)
        {
            lock (Lock)
            {
                if (!Items.TryGetValue(key, out var list))
                    return null;

                if (!list.Any())
                    return null;

                var item = list[0];
                list.RemoveAt(0);

                return item;
            }
        }

        internal static Connection Next(string key)
        {
            if (!Enabled)
                return null;

            ConnectionPoolItem item;
            while ((item = GetNext(key)) != null)
            {
                if (TestConnection(item))
                {
                    if (Debug) WriteDebug("#pool.next: revived connection " + item.Connection.ServerInfo.ConnectionId);
                    return item.Connection;
                }
            }

            return null;
        }

        private static bool TestConnection(ConnectionPoolItem item)
        {
            try
            {
                if (Debug) WriteDebug("#pool.test: testing connection " + item.Connection.ServerInfo.ConnectionId);

                item.Connection.SendCommand(Commands.Ping);
                var result = item.Connection.ReadPacket();
                if (!PacketReader.IsOkPacket(result))
                {
                    if (Debug) WriteDebug("#pool.test: test failed");
                    return false;
                }

                if (Debug) WriteDebug("#pool.test: test succeeded");
                return true;
            }
            catch (Exception e)
            {
                if (Debug) WriteDebug("#pool.test: test blew up: " + e.Message);
                return false;
            }
        }

        private static void WriteDebug(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
