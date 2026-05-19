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

    public ConnectionDispatcher(ServiceContext serviceContext, Func<T, Task> connectionDelegate, TransportConnectionManager transportConnectionManager)
    {
        _serviceContext = serviceContext;
        _connectionDelegate = connectionDelegate;
        _transportConnectionManager = transportConnectionManager;
    }

    private KestrelTrace Log => _serviceContext.Log;
    private KestrelMetrics Metrics => _serviceContext.Metrics;

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
                    var id = _transportConnectionManager.GetNewConnectionId();

                    // Cache counter enabled state at the start of the connection.
                    // This ensures that the state is consistent for the entire connection.
                    var metricsContext = Metrics.CreateContext(connection);

                    var kestrelConnection = new KestrelConnection<T>(
                        id, _serviceContext, _transportConnectionManager, _connectionDelegate, connection, Log, metricsContext);

                    _transportConnectionManager.AddConnection(id, kestrelConnection);

                    Log.ConnectionAccepted(connection.ConnectionId);
                    KestrelEventSource.Log.ConnectionQueuedStart(connection);
                    Metrics.ConnectionQueuedStart(metricsContext);

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
                _acceptLoopTcs.TrySetResult();
            }
        }
    }
}
