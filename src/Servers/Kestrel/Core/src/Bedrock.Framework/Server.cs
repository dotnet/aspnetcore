using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Connections
{
    public class Server
    {
        private readonly ServerOptions _serverOptions;
        private readonly ILogger<Server> _logger;
        private readonly List<RunningListener> _listeners = new List<RunningListener>();
        private readonly TaskCompletionSource<object> _shutdownTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TimerAwaitable _timerAwaitable;
        private Task _timerTask = Task.CompletedTask;

        public Server(ILoggerFactory loggerFactory, ServerOptions options)
        {
            _logger = loggerFactory.CreateLogger<Server>();
            _serverOptions = options ?? new ServerOptions();
            _timerAwaitable = new TimerAwaitable(_serverOptions.HeartBeatInterval, _serverOptions.HeartBeatInterval);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            foreach (var binding in _serverOptions.Bindings)
            {
                var listener = await binding.ConnectionListenerFactory.BindAsync(binding.EndPoint, cancellationToken);
                _logger.LogInformation("Listening on {address}", binding.EndPoint);
                binding.EndPoint = listener.EndPoint;

                var runningListener = new RunningListener(this, binding.EndPoint, listener, binding.Application);
                _listeners.Add(runningListener);
            }

            _timerAwaitable.Start();
            _timerTask = StartTimerAsync();
        }

        private async Task StartTimerAsync()
        {
            using (_timerAwaitable)
            {
                while (await _timerAwaitable)
                {
                    foreach (var listener in _listeners)
                    {
                        listener.TickHeartbeat();
                    }
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            var tasks = new Task[_listeners.Count];

            for (int i = 0; i < _listeners.Count; i++)
            {
                tasks[i] = _listeners[i].Listener.UnbindAsync(cancellationToken).AsTask();
            }

            await Task.WhenAll(tasks);

            // Signal to all of the listeners that it's time to start the shutdown process
            // We call this after unbind so that we're not touching the listener anymore (each loop will dispose the listener)
            _shutdownTcs.TrySetResult(null);

            for (int i = 0; i < _listeners.Count; i++)
            {
                tasks[i] = _listeners[i].ExecutionTask;
            }

            var shutdownTask = Task.WhenAll(tasks);

            if (cancellationToken.CanBeCanceled)
            {
                await shutdownTask.WithCancellation(cancellationToken);
            }
            else
            {
                await shutdownTask;
            }

            _timerAwaitable.Stop();

            await _timerTask;
        }

        private class RunningListener
        {
            private readonly Server _server;
            private readonly ConcurrentDictionary<long, (ServerConnection Connection, Task ExecutionTask)> _connections = new ConcurrentDictionary<long, (ServerConnection, Task)>();

            public RunningListener(Server server, EndPoint endpoint, IConnectionListener listener, ConnectionDelegate connectionDelegate)
            {
                _server = server;
                Listener = listener;
                ExecutionTask = RunListenerAsync(endpoint, listener, connectionDelegate);
            }

            public IConnectionListener Listener { get; }
            public Task ExecutionTask { get; }

            public void TickHeartbeat()
            {
                foreach (var pair in _connections)
                {
                    pair.Value.Connection.TickHeartbeat();
                }
            }

            private async Task RunListenerAsync(EndPoint endpoint, IConnectionListener listener, ConnectionDelegate connectionDelegate)
            {
                async Task ExecuteConnectionAsync(ServerConnection serverConnection)
                {
                    await Task.Yield();

                    var connection = serverConnection.TransportConnection;

                    try
                    {
                        using var scope = BeginConnectionScope(connection);

                        await connectionDelegate(connection);
                    }
                    catch (ConnectionAbortedException)
                    {
                        // Don't let connection aborted exceptions out
                    }
                    catch (Exception ex)
                    {
                        _server._logger.LogError(ex, "Unexpected exception from connection {ConnectionId}", connection.ConnectionId);
                    }
                    finally
                    {
                        // Fire the OnCompleted callbacks
                        await serverConnection.FireOnCompletedAsync();

                        await connection.DisposeAsync();

                        // Remove the connection from tracking
                        _connections.TryRemove(serverConnection.Id, out _);
                    }
                }

                long id = 0;

                while (true)
                {
                    try
                    {
                        var connection = await listener.AcceptAsync();

                        if (connection == null)
                        {
                            // Null means we don't have anymore connections
                            break;
                        }

                        var serverConnection = new ServerConnection(id, connection, _server._logger);

                        _connections[id] = (serverConnection, ExecuteConnectionAsync(serverConnection));
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _server._logger.LogCritical(ex, "Stopped accepting connections on {endpoint}", endpoint);
                        break;
                    }

                    id++;
                }

                // Don't shut down connections until entire server is shutting down
                await _server._shutdownTcs.Task;

                // Give connections a chance to close gracefully
                var tasks = new List<Task>(_connections.Count);

                foreach (var pair in _connections)
                {
                    pair.Value.Connection.RequestClose();
                    tasks.Add(pair.Value.ExecutionTask);
                }

                if (!await Task.WhenAll(tasks).TimeoutAfter(_server._serverOptions.GracefulShutdownTimeout))
                {
                    // Abort all connections still in flight
                    foreach (var pair in _connections)
                    {
                        pair.Value.Connection.TransportConnection.Abort();
                    }

                    await Task.WhenAll(tasks);
                }

                await listener.DisposeAsync();
            }


            private IDisposable BeginConnectionScope(ConnectionContext connectionContext)
            {
                if (_server._logger.IsEnabled(LogLevel.Critical))
                {
                    return _server._logger.BeginScope(new ConnectionLogScope(connectionContext.ConnectionId));
                }

                return null;
            }
        }
    }
}
