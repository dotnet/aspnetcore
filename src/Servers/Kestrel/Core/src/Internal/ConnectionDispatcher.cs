// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class ConnectionDispatcher<T> where T : BaseConnectionContext
    {
        private static long _lastConnectionId = long.MinValue;

        private readonly ServiceContext _serviceContext;
        private readonly Func<T, Task> _connectionDelegate;
        private readonly TransportConnectionManager _transportConnectionManager;
        private readonly TaskCompletionSource<object> _acceptLoopTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConnectionDispatcher(ServiceContext serviceContext, Func<T, Task> connectionDelegate, TransportConnectionManager transportConnectionManager)
        {
            _serviceContext = serviceContext;
            _connectionDelegate = connectionDelegate;
            _transportConnectionManager = transportConnectionManager;
        }

        private IKestrelTrace Log => _serviceContext.Log;

        public Task StartAcceptingConnections(IConnectionListener<T> listener)
        {
            ThreadPool.UnsafeQueueUserWorkItem(StartAcceptingConnectionsCore, listener, preferLocal: false);
            return _acceptLoopTcs.Task;
        }

        private void StartAcceptingConnectionsCore(IConnectionListener<T> listener)
        {
            // REVIEW: Multiple accept loops in parallel?
            _ = AcceptConnectionsAsync();

            async Task AcceptConnectionsAsync()
            {
                try
                {
                    while (true)
                    {
                        var connection = await listener.AcceptAsync();

                        if (connection == null)
                        {
                            // We're done listening
                            break;
                        }

                        // Add the connection to the connection manager before we queue it for execution
                        var id = Interlocked.Increment(ref _lastConnectionId);
                        var kestrelConnection = new KestrelConnection<T>(
                            id, _serviceContext, _transportConnectionManager, _connectionDelegate, connection, Log);

                        _transportConnectionManager.AddConnection(id, kestrelConnection);

                        Log.ConnectionAccepted(connection.ConnectionId);

                        ThreadPool.UnsafeQueueUserWorkItem(kestrelConnection, preferLocal: false);
                    }
                }
                catch (Exception ex)
                {
                    // REVIEW: If the accept loop ends should this trigger a server shutdown? It will manifest as a hang
                    Log.LogCritical(0, ex, "The connection listener failed to accept any new connections.");
                }
                finally
                {
                    _acceptLoopTcs.TrySetResult(null);
                }
            }
        }
    }
}
