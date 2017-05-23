// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionList : IReadOnlyCollection<ConnectionContext>
    {
        private readonly ConcurrentDictionary<string, ConnectionContext> _connections = new ConcurrentDictionary<string, ConnectionContext>();

        public ConnectionContext this[string connectionId]
        {
            get
            {
                ConnectionContext connection;
                if (_connections.TryGetValue(connectionId, out connection))
                {
                    return connection;
                }
                return null;
            }
        }

        public int Count => _connections.Count;

        public void Add(ConnectionContext connection)
        {
            _connections.TryAdd(connection.ConnectionId, connection);
        }

        public void Remove(ConnectionContext connection)
        {
            ConnectionContext dummy;
            _connections.TryRemove(connection.ConnectionId, out dummy);
        }

        public IEnumerator<ConnectionContext> GetEnumerator()
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
