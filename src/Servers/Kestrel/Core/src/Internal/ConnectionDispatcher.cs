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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0039:Use local function", Justification = "Delegate used deliberately for lifetime/allocation reasons")]
    private void StartAcceptingConnectionsCore(IConnectionListener<T> listener)
    {
        int concurrency = Math.Clamp(_maxAccepts, 1, listener.MaxAccepts), exited = 0;
        Console.WriteLine($"concurrency: {concurrency}; listener: {listener.GetType().FullName}");
        if (listener.SupportAcceptMany)
        {
            ThreadStart start = () =>  listener.AcceptMany(AddAndStartConnection);
            for (var i = 0; i < concurrency; i++)
            {
                new Thread(start)
                {
                    IsBackground = true,
                    Name = "Listener",
                }.Start();
            }
        }
        else
        {
            for (var i = 0; i < concurrency; i++)
            {
                _ = AcceptConcurrentConnectionsAsync();
                // _ = AcceptSimpleConnectionsAsync();
            }
        }

        async Task AcceptConcurrentConnectionsAsync()
        {
            try
            {
                Action<object> acceptAddAndStart = token =>
                {
                    var connection = listener.Accept(token);
                    AddAndStartConnection(connection);
                };
                await foreach (var token in listener.AcceptManyAsync())
                {
                    // push everything (including final setup steps, connection id, connection-mananger, etc) to the pool,
                    // so we can get back to listening
                    ThreadPool.UnsafeQueueUserWorkItem(acceptAddAndStart, token, preferLocal: false);
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

        //async Task AcceptSimpleConnectionsAsync()
        //{
        //    try
        //    {
        //        Action<T> addAndStart = connection => _ = AddAndStartConnection(connection);

        //        while (true)
        //        {
        //            var connection = await listener.AcceptAsync();

        //            if (connection == null)
        //            {
        //                // We're done listening
        //                break;
        //            }

        //            // push everything (including connection id, connection-mananger, etc) to the pool,
        //            // so we can get back to listening
        //            ThreadPool.UnsafeQueueUserWorkItem(addAndStart, connection, preferLocal: false);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // REVIEW: If the accept loop ends should this trigger a server shutdown? It will manifest as a hang
        //        Log.LogCritical(0, ex, "The connection listener failed to accept any new connections.");
        //    }
        //    finally
        //    {
        //        // listener has exited
        //        _acceptLoopTcs.TrySetResult();
        //    }
        //}
    }

    private void AddAndStartConnection(T connection)
    {
        // Add the connection to the connection manager before we queue it for execution
        var id = _transportConnectionManager.GetNewConnectionId();
        var kestrelConnection = new KestrelConnection<T>(
            id, _serviceContext, _transportConnectionManager, _connectionDelegate, connection, Log);

        _transportConnectionManager.AddConnection(id, kestrelConnection);

        Log.ConnectionAccepted(connection.ConnectionId);
        KestrelEventSource.Log.ConnectionQueuedStart(connection);
        _ = kestrelConnection.ExecuteAsync();
    }
}
