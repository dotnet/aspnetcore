using System;
using System.Collections.Concurrent;
using System.Threading;
using Channels;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionManager
    {
        private ConcurrentDictionary<string, ConnectionState> _connections = new ConcurrentDictionary<string, ConnectionState>();
        private Timer _timer;

        public ConnectionManager()
        {
            _timer = new Timer(Scan, this, 0, 1000);
        }

        public bool TryGetConnection(string id, out ConnectionState state)
        {
            return _connections.TryGetValue(id, out state);
        }

        public ConnectionState ReserveConnection()
        {
            string id = MakeNewConnectionId();

            // REVIEW: Should we create state for this?
            var state = _connections.GetOrAdd(id, connectionId => new ConnectionState());

            // Mark it as a reservation
            state.Connection = new Connection
            {
                ConnectionId = id
            };
            return state;
        }

        public ConnectionState AddNewConnection(IChannel channel)
        {
            string id = MakeNewConnectionId();

            var state = new ConnectionState
            {
                Connection = new Connection
                {
                    Channel = channel,
                    ConnectionId = id
                },
                LastSeen = DateTimeOffset.UtcNow,
                Active = true
            };

            _connections.TryAdd(id, state);
            return state;
        }

        public void MarkConnectionInactive(string id)
        {
            ConnectionState state;
            if (_connections.TryGetValue(id, out state))
            {
                // Mark the connection as active so the background thread can look at it
                state.Active = false;
            }
        }

        public void RemoveConnection(string id)
        {
            ConnectionState state;
            _connections.TryRemove(id, out state);

            // Remove the connection completely
        }

        private static string MakeNewConnectionId()
        {
            // TODO: We need to sign and encyrpt this
            return Guid.NewGuid().ToString();
        }

        private static void Scan(object state)
        {
            ((ConnectionManager)state).Scan();
        }

        private void Scan()
        {
            // Scan the registered connections looking for ones that have timed out
            foreach (var c in _connections)
            {
                if (!c.Value.Active && (DateTimeOffset.UtcNow - c.Value.LastSeen).TotalSeconds > 30)
                {
                    ConnectionState s;
                    if (_connections.TryRemove(c.Key, out s))
                    {
                        s.Connection.Channel.Dispose();
                    }
                    else
                    {

                    }
                }
            }
        }
    }
}
