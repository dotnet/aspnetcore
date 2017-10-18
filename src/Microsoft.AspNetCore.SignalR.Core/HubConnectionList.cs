// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    public class HubConnectionList : IReadOnlyCollection<HubConnectionContext>
    {
        private readonly ConcurrentDictionary<string, HubConnectionContext> _connections = new ConcurrentDictionary<string, HubConnectionContext>();

        public HubConnectionContext this[string connectionId]
        {
            get
            {
                _connections.TryGetValue(connectionId, out var connection);
                return connection;
            }
        }

        public int Count => _connections.Count;

        public void Add(HubConnectionContext connection)
        {
            _connections.TryAdd(connection.ConnectionId, connection);
        }

        public void Remove(HubConnectionContext connection)
        {
            _connections.TryRemove(connection.ConnectionId, out _);
        }

        public IEnumerator<HubConnectionContext> GetEnumerator()
        {
            return _connections.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
