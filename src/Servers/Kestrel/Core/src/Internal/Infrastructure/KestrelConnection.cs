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
    internal abstract class KestrelConnection : IConnectionHeartbeatFeature, IConnectionCompleteFeature, IConnectionLifetimeNotificationFeature
    {
        private List<(Action<object> handler, object state)>? _heartbeatHandlers;
        private readonly object _heartbeatLock = new object();

        private EventStack _onCompleted = new(0);
        private bool _completed;

        private readonly CancellationTokenSource _connectionClosingCts = new CancellationTokenSource();
        private readonly TaskCompletionSource _completionTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        protected readonly long _id;
        protected readonly ServiceContext _serviceContext;
        protected readonly TransportConnectionManager _transportConnectionManager;

        public KestrelConnection(long id,
                                 ServiceContext serviceContext,
                                 TransportConnectionManager transportConnectionManager,
                                 IKestrelTrace logger)
        {
            _id = id;
            _serviceContext = serviceContext;
            _transportConnectionManager = transportConnectionManager;
            Logger = logger;

            ConnectionClosedRequested = _connectionClosingCts.Token;
        }

        protected IKestrelTrace Logger { get; }

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

        public abstract BaseConnectionContext TransportConnection { get; }

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

            _onCompleted.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
        }

        public Task FireOnCompletedAsync()
        {
            if (_completed)
            {
                throw new InvalidOperationException("The connection is already complete.");
            }

            _completed = true;

            if (_onCompleted.Count == 0)
            {
                return Task.CompletedTask;
            }

            return CompleteAsyncMayAwait();
        }

        private Task CompleteAsyncMayAwait()
        {
            while (_onCompleted.TryPop(out var entry))
            {
                try
                {
                    var task = entry.Key.Invoke(entry.Value);
                    if (!task.IsCompletedSuccessfully)
                    {
                        return CompleteAsyncAwaited(task);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
                }
            }

            return Task.CompletedTask;
        }

        private async Task CompleteAsyncAwaited(Task currentTask)
        {
            try
            {
                await currentTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
            }

            while (_onCompleted.TryPop(out var entry))
            {
                try
                {
                    await entry.Key.Invoke(entry.Value);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
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
            _completionTcs.TrySetResult();

            _connectionClosingCts.Dispose();
        }

        protected IDisposable? BeginConnectionScope(BaseConnectionContext connectionContext)
        {
            if (Logger.IsEnabled(LogLevel.Critical))
            {
                return Logger.BeginScope(new ConnectionLogScope(connectionContext.ConnectionId));
            }

            return null;
        }
    }
}
