// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionManager
    {
        private readonly ConcurrentDictionary<string, DefaultConnectionContext> _connections = new ConcurrentDictionary<string, DefaultConnectionContext>();
        private Timer _timer;
        private readonly ILogger<ConnectionManager> _logger;
        private object _executionLock = new object();
        private bool _disposed;

        public ConnectionManager(ILogger<ConnectionManager> logger)
        {
            _logger = logger;
        }

        public void Start()
        {
            lock (_executionLock)
            {
                if (_disposed)
                {
                    return;
                }

                if (_timer == null)
                {
                    _timer = new Timer(Scan, this, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                }
            }
        }

        public bool TryGetConnection(string id, out DefaultConnectionContext connection)
        {
            return _connections.TryGetValue(id, out connection);
        }

        public DefaultConnectionContext CreateConnection()
        {
            var id = MakeNewConnectionId();

            var transportToApplication = Channel.CreateUnbounded<byte[]>();
            var applicationToTransport = Channel.CreateUnbounded<byte[]>();

            var transportSide = new ChannelConnection<byte[]>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<byte[]>(transportToApplication, applicationToTransport);

            var connection = new DefaultConnectionContext(id, applicationSide, transportSide);
            
            _connections.TryAdd(id, connection);
            return connection;
        }

        public void RemoveConnection(string id)
        {
            if (_connections.TryRemove(id, out _))
            {
                // Remove the connection completely
                _logger.LogDebug("Removing {connectionId} from the list of connections", id);
            }
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

        public void Scan()
        {
            // If we couldn't get the lock it means one of 2 things are true:
            // - We're about to dispose so we don't care to run the scan callback anyways.
            // - The previous Scan took long enough that the next scan tried to run in parallel
            // In either case just do nothing and end the timer callback as soon as possible
            if (!Monitor.TryEnter(_executionLock))
            {
                return;
            }

            try
            {
                if (_disposed || Debugger.IsAttached)
                {
                    return;
                }

                // Pause the timer while we're running
                _timer.Change(Timeout.Infinite, Timeout.Infinite);

                // Scan the registered connections looking for ones that have timed out
                foreach (var c in _connections)
                {
                    var status = DefaultConnectionContext.ConnectionStatus.Inactive;
                    var lastSeenUtc = DateTimeOffset.UtcNow;

                    try
                    {
                        c.Value.Lock.Wait();

                        // Capture the connection state
                        status = c.Value.Status;

                        lastSeenUtc = c.Value.LastSeenUtc;
                    }
                    finally
                    {
                        c.Value.Lock.Release();
                    }

                    // Once the decision has been made to to dispose we don't check the status again
                    if (status == DefaultConnectionContext.ConnectionStatus.Inactive && (DateTimeOffset.UtcNow - lastSeenUtc).TotalSeconds > 5)
                    {
                        var ignore = DisposeAndRemoveAsync(c.Value);
                    }
                }

                // Resume once we finished processing all connections
                _timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }
            finally
            {
                // Exit the lock now
                Monitor.Exit(_executionLock);
            }
        }

        public void CloseConnections()
        {
            lock (_executionLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                // Stop firing the timer
                _timer?.Dispose();

                var tasks = new List<Task>();

                foreach (var c in _connections)
                {
                    tasks.Add(DisposeAndRemoveAsync(c.Value));
                }

                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(5));
            }
        }

        public async Task DisposeAndRemoveAsync(DefaultConnectionContext connection)
        {
            try
            {
                await connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Failed disposing connection {connectionId}", connection.ConnectionId);
            }
            finally
            {
                // Remove it from the list after disposal so that's it's easy to see
                // connections that might be in a hung state via the connections list
                RemoveConnection(connection.ConnectionId);
            }
        }
    }
}
