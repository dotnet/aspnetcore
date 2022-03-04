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
    internal class ConnectionDispatcher
    {
        private static long _lastConnectionId = long.MinValue;

        private readonly ServiceContext _serviceContext;
        private readonly ConnectionDelegate _connectionDelegate;
        private readonly TaskCompletionSource<object> _acceptLoopTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConnectionDispatcher(ServiceContext serviceContext, ConnectionDelegate connectionDelegate)
        {
            _serviceContext = serviceContext;
            _connectionDelegate = connectionDelegate;
        }

        private IKestrelTrace Log => _serviceContext.Log;

        public Task StartAcceptingConnections(IConnectionListener listener)
        {
            ThreadPool.UnsafeQueueUserWorkItem(StartAcceptingConnectionsCore, listener, preferLocal: false);
            return _acceptLoopTcs.Task;
        }

        private void StartAcceptingConnectionsCore(IConnectionListener listener)
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

                        _ = Execute(new KestrelConnection(connection, _serviceContext.Log));
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

        internal async Task Execute(KestrelConnection connection)
        {
            var id = Interlocked.Increment(ref _lastConnectionId);
            var connectionContext = connection.TransportConnection;

            try
            {
                _serviceContext.ConnectionManager.AddConnection(id, connection);

                Log.ConnectionStart(connectionContext.ConnectionId);
                KestrelEventSource.Log.ConnectionStart(connectionContext);

                using (BeginConnectionScope(connectionContext))
                {
                    try
                    {
                        await _connectionDelegate(connectionContext);
                    }
                    catch (Exception ex)
                    {
                        Log.LogError(0, ex, "Unhandled exception while processing {ConnectionId}.", connectionContext.ConnectionId);
                    }
                }
            }
            finally
            {
                await connection.FireOnCompletedAsync();

                Log.ConnectionStop(connectionContext.ConnectionId);
                KestrelEventSource.Log.ConnectionStop(connectionContext);

                // Dispose the transport connection, this needs to happen before removing it from the
                // connection manager so that we only signal completion of this connection after the transport
                // is properly torn down.
                await connection.TransportConnection.DisposeAsync();

                _serviceContext.ConnectionManager.RemoveConnection(id);
            }
        }

        private IDisposable BeginConnectionScope(ConnectionContext connectionContext)
        {
            if (Log.IsEnabled(LogLevel.Critical))
            {
                return Log.BeginScope(new ConnectionLogScope(connectionContext.ConnectionId));
            }

            return null;
        }
    }
}
