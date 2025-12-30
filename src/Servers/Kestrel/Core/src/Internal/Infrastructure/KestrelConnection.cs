// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal abstract class KestrelConnection : IConnectionHeartbeatFeature, IConnectionCompleteFeature, IConnectionLifetimeNotificationFeature, IConnectionMetricsContextFeature
{
    private List<(Action<object> handler, object state)>? _heartbeatHandlers;
    private readonly Lock _heartbeatLock = new();

    private Stack<KeyValuePair<Func<object, Task>, object>>? _onCompleted;
    private bool _completed;

    private readonly CancellationTokenSource _connectionClosingCts = new CancellationTokenSource();
    private readonly TaskCompletionSource _completionTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly long _id;
    protected readonly ServiceContext _serviceContext;
    protected readonly TransportConnectionManager _transportConnectionManager;

    public KestrelConnection(long id,
                             ServiceContext serviceContext,
                             TransportConnectionManager transportConnectionManager,
                             KestrelTrace logger,
                             ConnectionMetricsContext connectionMetricsContext)
    {
        _id = id;
        _serviceContext = serviceContext;
        _transportConnectionManager = transportConnectionManager;
        Logger = logger;
        MetricsContext = connectionMetricsContext;
        ConnectionClosedRequested = _connectionClosingCts.Token;
    }

    protected KestrelTrace Logger { get; }

    public ConnectionMetricsContext MetricsContext { get; set; }
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
                Logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
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
            Logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
        }

        while (onCompleted.TryPop(out var entry))
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
