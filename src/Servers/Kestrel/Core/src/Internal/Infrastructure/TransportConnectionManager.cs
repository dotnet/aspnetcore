// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

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
                throw new ArgumentException("Unable to add specified id.", nameof(id));
            }

            _connectionManager.AddConnection(id, connectionReference);
        }

        public void RemoveConnection(long id)
        {
            if (!_connectionReferences.TryRemove(id, out _))
            {
                throw new ArgumentException("No value found for the specified id.", nameof(id));
            }

            _connectionManager.RemoveConnection(id);
        }

        // This is only called by the ConnectionManager when the connection reference becomes
        // unrooted because the application never completed.
        public void StopTracking(long id)
        {
            if (!_connectionReferences.TryRemove(id, out _))
            {
                throw new ArgumentException("No value found for the specified id.", nameof(id));
            }
        }

        public async Task<bool> CloseAllConnectionsAsync(CancellationToken token)
        {
            var closeTasks = new List<Task>();

            foreach (var kvp in _connectionReferences)
            {
                if (kvp.Value.TryGetConnection(out var connection))
                {
                    connection.RequestClose();
                    closeTasks.Add(connection.ExecutionTask);
                }
            }

            var allClosedTask = Task.WhenAll(closeTasks.ToArray());
            return await Task.WhenAny(allClosedTask, CancellationTokenAsTask(token)).ConfigureAwait(false) == allClosedTask;
        }

        public async Task<bool> AbortAllConnectionsAsync()
        {
            var abortTasks = new List<Task>();

            foreach (var kvp in _connectionReferences)
            {
                if (kvp.Value.TryGetConnection(out var connection))
                {
                    connection.TransportConnection.Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedDuringServerShutdown));
                    abortTasks.Add(connection.ExecutionTask);
                }
            }

            var allAbortedTask = Task.WhenAll(abortTasks.ToArray());
            return await Task.WhenAny(allAbortedTask, Task.Delay(1000)).ConfigureAwait(false) == allAbortedTask;
        }

        private static Task CancellationTokenAsTask(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            token.Register(() => tcs.SetResult());
            return tcs.Task;
        }
    }
}
