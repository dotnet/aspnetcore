// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionManager
    {
        private ConcurrentDictionary<string, ConnectionState> _connections = new ConcurrentDictionary<string, ConnectionState>();
        private Timer _timer;

        public ConnectionManager(IApplicationLifetime lifetime)
        {
            _timer = new Timer(Scan, this, 0, 1000);

            // We hook stopping because we need the requests to end, Dispose doesn't work since
            // that happens after requests are drained
            lifetime.ApplicationStopping.Register(CloseConnections);
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

        public ConnectionState AddNewConnection(IPipelineConnection connection)
        {
            string id = MakeNewConnectionId();

            var state = new ConnectionState
            {
                Connection = new Connection
                {
                    Channel = connection,
                    ConnectionId = id
                },
                LastSeen = DateTimeOffset.UtcNow,
                Active = true
            };

            _connections.TryAdd(id, state);
            return state;
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
                if (!c.Value.Active && (DateTimeOffset.UtcNow - c.Value.LastSeen).TotalSeconds > 5)
                {
                    ConnectionState s;
                    if (_connections.TryRemove(c.Key, out s))
                    {
                        s?.Close();
                    }
                    else
                    {

                    }
                }
            }
        }

        private void CloseConnections()
        {
            // Stop firing the timer
            _timer.Dispose();

            foreach (var c in _connections)
            {
                ConnectionState s;
                if (_connections.TryRemove(c.Key, out s))
                {
                    // Longpolling connections should do this
                    if (s.Close != null)
                    {
                        s.Close();
                    }
                    else
                    {
                        s.Connection.Channel.Dispose();
                    }
                }
            }
        }
    }
}
