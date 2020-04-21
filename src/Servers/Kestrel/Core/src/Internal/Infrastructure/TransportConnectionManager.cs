// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class TransportConnectionManager
    {
        private readonly ConnectionManager _connectionManager;
        private readonly ConcurrentDictionary<long, ConnectionReference> _connectionReferences = new ConcurrentDictionary<long, ConnectionReference>();

        public TransportConnectionManager(ConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public void AddConnection(long id, KestrelConnection connection)
        {
            var connectionReference = new ConnectionReference(id, connection, this);

            if (!_connectionReferences.TryAdd(id, connectionReference))
            {
                throw new ArgumentException(nameof(id));
            }

            _connectionManager.AddConnection(id, connectionReference);
        }

        public void RemoveConnection(long id)
        {
            if (!_connectionReferences.TryRemove(id, out _))
            {
                throw new ArgumentException(nameof(id));
            }

            _connectionManager.RemoveConnection(id);
        }

        // This is only called by the ConnectionManager when the connection reference becomes
        // unrooted because the application never completed.
        public void StopTracking(long id)
        {
            if (!_connectionReferences.TryRemove(id, out _))
            {
                throw new ArgumentException(nameof(id));
            }
        }

        public Task<bool> CloseAllConnectionsAsync(CancellationToken token)
        {
            return ConnectionManager.CloseAllConnectionsAsync(_connectionReferences, token);
        }

        public Task<bool> AbortAllConnectionsAsync()
        {
            return ConnectionManager.AbortAllConnectionsAsync(_connectionReferences);
        }
    }
}
