using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace WebApplication95
{
    public class ConnectionManager
    {
        private ConcurrentDictionary<string, ConnectionState> _connections = new ConcurrentDictionary<string, ConnectionState>();
        private readonly ChannelFactory _channelFactory = new ChannelFactory();
        private Timer _timer;

        public ConnectionManager()
        {
            _timer = new Timer(Scan, this, 0, 1000);
        }

        private static void Scan(object state)
        {
            ((ConnectionManager)state).Scan();
        }

        private void Scan()
        {
            foreach (var c in _connections)
            {
                if (!c.Value.Alive && (DateTimeOffset.UtcNow - c.Value.LastSeen).TotalSeconds > 30)
                {
                    ConnectionState s;
                    if (_connections.TryRemove(c.Key, out s))
                    {
                        s.Connection.Complete();
                    }
                    else
                    {

                    }
                }
            }
        }


        // TODO: don't leak HttpContext to ConnectionManager
        public string GetConnectionId(HttpContext context)
        {
            var id = context.Request.Query["id"];

            if (!StringValues.IsNullOrEmpty(id))
            {
                return id.ToString();
            }

            return Guid.NewGuid().ToString();
        }

        public bool TryGetConnection(string id, out ConnectionState state)
        {
            return _connections.TryGetValue(id, out state);
        }

        public bool AddConnection(string id, out ConnectionState state)
        {
            state = _connections.GetOrAdd(id, connectionId => new ConnectionState());
            var isNew = state.Connection == null;
            if (isNew)
            {
                state.Connection = new Connection
                {
                    ConnectionId = id,
                    Input = _channelFactory.CreateChannel(),
                    Output = _channelFactory.CreateChannel()
                };
            }
            state.LastSeen = DateTimeOffset.UtcNow;
            state.Alive = true;
            return isNew;
        }

        public void MarkConnectionDead(string id)
        {
            ConnectionState state;
            if (_connections.TryGetValue(id, out state))
            {
                state.Alive = false;
            }
        }

        public void RemoveConnection(string id)
        {
            ConnectionState state;
            if (_connections.TryRemove(id, out state))
            {

            }
        }
    }
}
