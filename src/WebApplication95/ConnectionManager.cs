using System;
using System.Collections.Concurrent;
using System.Threading;
using Channels;

namespace WebApplication95
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

            var state = _connections.GetOrAdd(id, connectionId => new ConnectionState());

            // If there's no connection object then it's a new connection
            if (state.Connection == null)
            {
                state.Connection = new Connection
                {
                    Channel = channel,
                    ConnectionId = id
                };
            }

            // Update the last seen and mark the connection as active
            state.LastSeen = DateTimeOffset.UtcNow;
            state.Active = true;
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
