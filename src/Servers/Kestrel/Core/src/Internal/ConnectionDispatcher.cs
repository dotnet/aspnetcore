// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class ConnectionDispatcher<T> where T : BaseConnectionContext
{
    private readonly ServiceContext _serviceContext;
    private readonly Func<T, Task> _connectionDelegate;
    private readonly TransportConnectionManager _transportConnectionManager;
    private readonly TaskCompletionSource _acceptLoopTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly int _maxAccepts;

    public ConnectionDispatcher(ServiceContext serviceContext, Func<T, Task> connectionDelegate, TransportConnectionManager transportConnectionManager,
        ListenOptions listenOptions)
    {
        _serviceContext = serviceContext;
        _connectionDelegate = connectionDelegate;
        _transportConnectionManager = transportConnectionManager;
        _maxAccepts = listenOptions.MaxAccepts;
    }

    private KestrelTrace Log => _serviceContext.Log;

    public Task StartAcceptingConnections(IConnectionListener<T> listener)
    {
        ThreadPool.UnsafeQueueUserWorkItem(StartAcceptingConnectionsCore, listener, preferLocal: false);
        return _acceptLoopTcs.Task;
    }

    private void StartAcceptingConnectionsCore(IConnectionListener<T> listener)
    {
        int concurrency = 1, exited = 0;
        if (listener is IConcurrentConnectionListener concurrent)
        {
            concurrency = Math.Clamp(_maxAccepts, 1, concurrent.MaxAccepts);
        }

        for (var i = 0; i < concurrency; i++)
        {
            _ = AcceptConnectionsAsync();
        }

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
                    var id = _transportConnectionManager.GetNewConnectionId();
                    var kestrelConnection = new KestrelConnection<T>(
                        id, _serviceContext, _transportConnectionManager, _connectionDelegate, connection, Log);

                    _transportConnectionManager.AddConnection(id, kestrelConnection);

                    Log.ConnectionAccepted(connection.ConnectionId);
                    KestrelEventSource.Log.ConnectionQueuedStart(connection);

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
                // don't decr concurrency - that has a race if things fail
                // while we're still spinning up; instead, track exited count
                // explicitly, and compare to that
                if (Interlocked.Increment(ref exited) == concurrency)
                {   // last listener has dropped
                    _acceptLoopTcs.TrySetResult();
                }
            }
        }
    }
}
