// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class KestrelConnection : IConnectionHeartbeatFeature, IConnectionCompleteFeature, IConnectionLifetimeNotificationFeature, IThreadPoolWorkItem
    {
        private List<(Action<object> handler, object state)> _heartbeatHandlers;
        private readonly object _heartbeatLock = new object();

        private Stack<KeyValuePair<Func<object, Task>, object>> _onCompleted;
        private bool _completed;

        private readonly CancellationTokenSource _connectionClosingCts = new CancellationTokenSource();
        private readonly TaskCompletionSource<object> _completionTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly long _id;
        private readonly ServiceContext _serviceContext;
        private readonly ConnectionDelegate _connectionDelegate;

        public KestrelConnection(long id,
                                 ServiceContext serviceContext,
                                 ConnectionDelegate connectionDelegate,
                                 ConnectionContext connectionContext,
                                 IKestrelTrace logger)
        {
            _id = id;
            _serviceContext = serviceContext;
            _connectionDelegate = connectionDelegate;
            Logger = logger;
            TransportConnection = connectionContext;

            connectionContext.Features.Set<IConnectionHeartbeatFeature>(this);
            connectionContext.Features.Set<IConnectionCompleteFeature>(this);
            connectionContext.Features.Set<IConnectionLifetimeNotificationFeature>(this);
            ConnectionClosedRequested = _connectionClosingCts.Token;
        }

        private IKestrelTrace Logger { get; }

        public ConnectionContext TransportConnection { get; set; }
        public CancellationToken ConnectionClosedRequested { get; set; }
        public Task ExecutionTask => _completionTcs.Task;

        public void TickHeartbeat()
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    return;
                }

                foreach (var (handler, state) in _heartbeatHandlers)
                {
                    handler(state);
                }
            }
        }

        public void OnHeartbeat(Action<object> action, object state)
        {
            lock (_heartbeatLock)
            {
                if (_heartbeatHandlers == null)
                {
                    _heartbeatHandlers = new List<(Action<object> handler, object state)>();
                }

                _heartbeatHandlers.Add((action, state));
            }
        }

        void IConnectionCompleteFeature.OnCompleted(Func<object, Task> callback, object state)
        {
            if (_completed)
            {
                throw new InvalidOperationException("The connection is already complete.");
            }

            if (_onCompleted == null)
            {
                _onCompleted = new Stack<KeyValuePair<Func<object, Task>, object>>();
            }
            _onCompleted.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
        }

        public Task FireOnCompletedAsync()
        {
            if (_completed)
            {
                throw new InvalidOperationException("The connection is already complete.");
            }

            _completed = true;
            var onCompleted = _onCompleted;

            if (onCompleted == null || onCompleted.Count == 0)
            {
                return Task.CompletedTask;
            }

            return CompleteAsyncMayAwait(onCompleted);
        }

        private Task CompleteAsyncMayAwait(Stack<KeyValuePair<Func<object, Task>, object>> onCompleted)
        {
            while (onCompleted.TryPop(out var entry))
            {
                try
                {
                    var task = entry.Key.Invoke(entry.Value);
                    if (!task.IsCompletedSuccessfully)
                    {
                        return CompleteAsyncAwaited(task, onCompleted);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error occured running an IConnectionCompleteFeature.OnCompleted callback.");
                }
            }

            return Task.CompletedTask;
        }

        private async Task CompleteAsyncAwaited(Task currentTask, Stack<KeyValuePair<Func<object, Task>, object>> onCompleted)
        {
            try
            {
                await currentTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occured running an IConnectionCompleteFeature.OnCompleted callback.");
            }

            while (onCompleted.TryPop(out var entry))
            {
                try
                {
                    await entry.Key.Invoke(entry.Value);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error occured running an IConnectionCompleteFeature.OnCompleted callback.");
                }
            }
        }

        public void RequestClose()
        {
            try
            {
                _connectionClosingCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // There's a race where the token could be disposed
                // swallow the exception and no-op
            }
        }

        public void Complete()
        {
            _completionTcs.TrySetResult(null);

            _connectionClosingCts.Dispose();
        }

        void IThreadPoolWorkItem.Execute()
        {
            _ = ExecuteAsync();
        }

        internal async Task ExecuteAsync()
        {
            var connectionContext = TransportConnection;

            try
            {
                Logger.ConnectionStart(connectionContext.ConnectionId);
                KestrelEventSource.Log.ConnectionStart(connectionContext);

                using (BeginConnectionScope(connectionContext))
                {
                    try
                    {
                        await _connectionDelegate(connectionContext);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(0, ex, "Unhandled exception while processing {ConnectionId}.", connectionContext.ConnectionId);
                    }
                }
            }
            finally
            {
                await FireOnCompletedAsync();

                Logger.ConnectionStop(connectionContext.ConnectionId);
                KestrelEventSource.Log.ConnectionStop(connectionContext);

                // Dispose the transport connection, this needs to happen before removing it from the
                // connection manager so that we only signal completion of this connection after the transport
                // is properly torn down.
                await TransportConnection.DisposeAsync();

                _serviceContext.ConnectionManager.RemoveConnection(_id);
            }
        }

        private IDisposable BeginConnectionScope(ConnectionContext connectionContext)
        {
            if (Logger.IsEnabled(LogLevel.Critical))
            {
                return Logger.BeginScope(new ConnectionLogScope(connectionContext.ConnectionId));
            }

            return null;
        }
    }
}
