// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class ConnectionManager
    {
        private readonly ConcurrentDictionary<long, ConnectionReference> _connectionReferences = new ConcurrentDictionary<long, ConnectionReference>();
        private readonly IKestrelTrace _trace;

        public ConnectionManager(IKestrelTrace trace, long? upgradedConnectionLimit)
            : this(trace, GetCounter(upgradedConnectionLimit))
        {
        }

        public ConnectionManager(IKestrelTrace trace, ResourceCounter upgradedConnections)
        {
            UpgradedConnectionCount = upgradedConnections;
            _trace = trace;
        }

        /// <summary>
        /// Connections that have been switched to a different protocol.
        /// </summary>
        public ResourceCounter UpgradedConnectionCount { get; }

        public void AddConnection(long id, ConnectionReference connectionReference)
        {
            if (!_connectionReferences.TryAdd(id, connectionReference))
            {
                throw new ArgumentException(nameof(id));
            }
        }

        public void RemoveConnection(long id)
        {
            if (!_connectionReferences.TryRemove(id, out var reference))
            {
                throw new ArgumentException(nameof(id));
            }

            if (reference.TryGetConnection(out var connection))
            {
                connection.Complete();
            }
        }

        public void Walk(Action<KestrelConnection> callback)
        {
            foreach (var kvp in _connectionReferences)
            {
                var reference = kvp.Value;

                if (reference.TryGetConnection(out var connection))
                {
                    callback(connection);
                }
                else if (_connectionReferences.TryRemove(kvp.Key, out reference))
                {
                    // It's safe to modify the ConcurrentDictionary in the foreach.
                    // The connection reference has become unrooted because the application never completed.
                    _trace.ApplicationNeverCompleted(reference.ConnectionId);
                    reference.StopTrasnsportTracking();
                }

                // If both conditions are false, the connection was removed during the heartbeat.
            }
        }

        public Task<bool> CloseAllConnectionsAsync(CancellationToken token)
        {
            return CloseAllConnectionsAsync(_connectionReferences, token);
        }

        public Task<bool> AbortAllConnectionsAsync()
        {
            return AbortAllConnectionsAsync(_connectionReferences);
        }

        internal static async Task<bool> CloseAllConnectionsAsync(ConcurrentDictionary<long, ConnectionReference> connectionReferences, CancellationToken token)
        {
            var closeTasks = new List<Task>();

            foreach (var kvp in connectionReferences)
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

        internal static async Task<bool> AbortAllConnectionsAsync(ConcurrentDictionary<long, ConnectionReference> connectionReferences)
        {
            var abortTasks = new List<Task>();

            foreach (var kvp in connectionReferences)
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

            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            token.Register(() => tcs.SetResult(null));
            return tcs.Task;
        }

        private static ResourceCounter GetCounter(long? number)
            => number.HasValue
                ? ResourceCounter.Quota(number.Value)
                : ResourceCounter.Unlimited;
    }
}
