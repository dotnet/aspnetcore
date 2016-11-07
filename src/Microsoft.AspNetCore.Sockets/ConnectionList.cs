using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionList : IReadOnlyCollection<Connection>
    {
        private readonly ConcurrentDictionary<string, Connection> _connections = new ConcurrentDictionary<string, Connection>();

        public Connection this[string connectionId]
        {
            get
            {
                Connection connection;
                if (_connections.TryGetValue(connectionId, out connection))
                {
                    return connection;
                }
                return null;
            }
        }

        public int Count => _connections.Count;

        public void Add(Connection connection)
        {
            _connections.TryAdd(connection.ConnectionId, connection);
        }

        public void Remove(Connection connection)
        {
            Connection dummy;
            _connections.TryRemove(connection.ConnectionId, out dummy);
        }

        public IEnumerator<Connection> GetEnumerator()
        {
            foreach (var item in _connections)
            {
                yield return item.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
