// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class ConnectionDispatcher
    {
        private static long _lastConnectionId = long.MinValue;

        private readonly ServiceContext _serviceContext;
        private readonly ConnectionDelegate _connectionDelegate;

        public ConnectionDispatcher(ServiceContext serviceContext, ConnectionDelegate connectionDelegate)
        {
            _serviceContext = serviceContext;
            _connectionDelegate = connectionDelegate;
        }

        private IKestrelTrace Log => _serviceContext.Log;

        public void StartAcceptingConnections(IConnectionListener listener)
        {
            ThreadPool.UnsafeQueueUserWorkItem(StartAcceptingConnectionsCore, listener, preferLocal: false);
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

                        // Set a connection id if the transport didn't set one
                        connection.ConnectionId ??= CorrelationIdGenerator.GetNextId();

                        // TODO: We need a bit of command and control to do connection management and that requires
                        // that we have access to a couple of methods that only exists on TransportConnection
                        // (specifically RequestClose, TickHeartbeat and CompleteAsync. This dependency needs to be removed or
                        // the return value of AcceptAsync needs to change.

                        _ = OnConnection((TransportConnection)connection);
                    }
                }
                catch (Exception)
                {
                    // REVIEW: Today the only way to end the accept loop is an exception
                }
            }
        }

        private async Task OnConnection(TransportConnection connection)
        {
            await using (connection)
            {
                await Execute(new KestrelConnection(connection));
            }
        }

        private async Task Execute(KestrelConnection connection)
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
                        Log.LogCritical(0, ex, $"{nameof(ConnectionDispatcher)}.{nameof(Execute)}() {connectionContext.ConnectionId}");
                    }
                    finally
                    {
                        // Complete the transport PipeReader and PipeWriter after calling into application code
                        connectionContext.Transport.Input.Complete();
                        connectionContext.Transport.Output.Complete();
                    }

                    // Wait for the transport to close
                    await CancellationTokenAsTask(connectionContext.ConnectionClosed);
                }
            }
            finally
            {
                await connectionContext.CompleteAsync();

                Log.ConnectionStop(connectionContext.ConnectionId);
                KestrelEventSource.Log.ConnectionStop(connectionContext);

                connection.Complete();

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

        private static Task CancellationTokenAsTask(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            // Transports already dispatch prior to tripping ConnectionClosed
            // since application code can register to this token.
            var tcs = new TaskCompletionSource<object>();
            token.Register(state => ((TaskCompletionSource<object>)state).SetResult(null), tcs);
            return tcs.Task;
        }
    }
}
