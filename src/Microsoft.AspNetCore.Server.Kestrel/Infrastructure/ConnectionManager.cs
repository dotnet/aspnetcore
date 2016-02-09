// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    public class ConnectionManager
    {
        private bool _managerClosed;
        private ConcurrentDictionary<long, Connection> _activeConnections = new ConcurrentDictionary<long, Connection>();

        public void AddConnection(long connectionId, Connection connection)
        {
            if (_managerClosed)
            {
                throw new InvalidOperationException(nameof(ConnectionManager) + " closed.");
            }

            if (!_activeConnections.TryAdd(connectionId, connection))
            {
                throw new InvalidOperationException("Connection already added.");
            }
        }

        public void ConnectionStopped(long connectionId)
        {
            Connection removed;
            _activeConnections.TryRemove(connectionId, out removed);
        } 

        public Task CloseConnectionsAsync()
        {
            if (_managerClosed)
            {
                throw new InvalidOperationException(nameof(ConnectionManager) + " already closed.");
            }

            _managerClosed = true;

            var stopTasks = new List<Task>();

            foreach (var connectionId in _activeConnections.Keys)
            {
                Connection removed;
                if (_activeConnections.TryRemove(connectionId, out removed))
                {
                    stopTasks.Add(removed.StopAsync());
                }
            }

            return Task.WhenAll(stopTasks);
        }
    }
}
