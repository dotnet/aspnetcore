// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionList<T> : IReadOnlyCollection<T> where T: Connection
    {
        private readonly ConcurrentDictionary<string, T> _connections = new ConcurrentDictionary<string, T>();

        public T this[string connectionId]
        {
            get
            {
                T connection;
                if (_connections.TryGetValue(connectionId, out connection))
                {
                    return connection;
                }
                return null;
            }
        }

        public int Count => _connections.Count;

        public void Add(T connection)
        {
            _connections.TryAdd(connection.ConnectionId, connection);
        }

        public void Remove(T connection)
        {
            T dummy;
            _connections.TryRemove(connection.ConnectionId, out dummy);
        }

        public IEnumerator<T> GetEnumerator()
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
