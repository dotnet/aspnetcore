// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class Http2PrefaceConnectionMiddleware
{
    private readonly ConnectionDelegate _next;
    private readonly HttpProtocols _endpointDefaultProtocols;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _keepAliveTimeout;
    private readonly KestrelTrace _log;
    private readonly IDebugger _debugger;

    public Http2PrefaceConnectionMiddleware(
        ConnectionDelegate next,
        ServiceContext serviceContext,
        HttpProtocols endpointDefaultProtocols,
        IDebugger? debugger = null)
    {
        _next = next;
        _endpointDefaultProtocols = endpointDefaultProtocols;
        _timeProvider = serviceContext.TimeProvider;
        _keepAliveTimeout = serviceContext.ServerOptions.Limits.KeepAliveTimeout;
        _log = serviceContext.Log;
        _debugger = debugger ?? DebuggerWrapper.Singleton;
    }

    public Task OnConnectionAsync(ConnectionContext connectionContext)
    {
        var protocols = connectionContext.Features.Get<HttpProtocolsFeature>()?.HttpProtocols ?? _endpointDefaultProtocols;

        if (connectionContext.Features.Get<ITlsConnectionFeature>() is not null ||
            !protocols.HasFlag(HttpProtocols.Http1) ||
            !protocols.HasFlag(HttpProtocols.Http2))
        {
            return _next(connectionContext);
        }

        return SelectProtocolAsync(connectionContext);
    }

    private async Task SelectProtocolAsync(ConnectionContext connectionContext)
    {
        var input = connectionContext.Transport.Input;
        var selectionState = new SelectionState(input, connectionContext, _timeProvider, _keepAliveTimeout, _debugger);
        var selectedProtocol = HttpProtocols.None;
        var shutdownRegistration = default(CancellationTokenRegistration);
        ITimer? timeoutTimer = null;

        try
        {
            var lifetimeNotificationFeature = connectionContext.Features.Get<IConnectionLifetimeNotificationFeature>();
            shutdownRegistration = lifetimeNotificationFeature?.ConnectionClosedRequested.UnsafeRegister(
                static state => ((SelectionState)state!).RequestShutdown(), selectionState) ?? default;
            timeoutTimer = selectionState.StartTimeoutTimer();

            while (true)
            {
                ReadResult result;
                try
                {
                    result = await input.ReadAsync(selectionState.ReadCancellationToken);
                }
                catch (Exception ex)
                {
                    if (!selectionState.TryComplete())
                    {
                        await CompleteStopAsync(selectionState.GetStopOperation()!, connectionContext);
                        return;
                    }

                    switch (ex)
                    {
                        case ConnectionResetException:
                            return;
                        case IOException:
                            _log.RequestProcessingError(connectionContext.ConnectionId, ex);
                            KestrelMetrics.AddConnectionEndReason(
                                connectionContext.Features.Get<IConnectionMetricsTagsFeature>(),
                                ConnectionEndReason.IOError);
                            return;
                        case ConnectionAbortedException:
                            _log.RequestProcessingError(connectionContext.ConnectionId, ex);
                            return;
                        default:
                            throw;
                    }
                }

                var buffer = result.Buffer;
                var examined = buffer.Start;
                var inputCompleted = false;
                ExceptionDispatchInfo? advanceException = null;

                try
                {
                    if (!buffer.IsEmpty)
                    {
                        var compareLength = (int)Math.Min(buffer.Length, Http2Connection.ClientPreface.Length);
                        var reader = new SequenceReader<byte>(buffer);

                        if (!reader.IsNext(Http2Connection.ClientPreface[..compareLength], advancePast: false))
                        {
                            selectedProtocol = HttpProtocols.Http1;
                        }
                        else if (buffer.Length >= Http2Connection.ClientPreface.Length)
                        {
                            selectedProtocol = HttpProtocols.Http2;
                        }
                        else if (result.IsCompleted)
                        {
                            selectedProtocol = HttpProtocols.Http1;
                        }
                        else
                        {
                            examined = buffer.End;
                        }
                    }
                    else if (result.IsCompleted)
                    {
                        inputCompleted = true;
                    }
                }
                finally
                {
                    try
                    {
                        input.AdvanceTo(buffer.Start, examined);
                    }
                    catch (Exception ex)
                    {
                        advanceException = ExceptionDispatchInfo.Capture(ex);
                    }
                }

                var stopOperation = selectionState.GetStopOperation();
                if (stopOperation is not null)
                {
                    await CompleteStopAsync(stopOperation, connectionContext);
                    return;
                }

                if (advanceException is not null)
                {
                    if (!selectionState.TryComplete())
                    {
                        await CompleteStopAsync(selectionState.GetStopOperation()!, connectionContext);
                        return;
                    }

                    advanceException.Throw();
                }

                if (inputCompleted)
                {
                    if (!selectionState.TryComplete())
                    {
                        await CompleteStopAsync(selectionState.GetStopOperation()!, connectionContext);
                    }
                    return;
                }

                if (selectedProtocol != HttpProtocols.None && selectionState.TrySelect())
                {
                    connectionContext.Features.Set(new HttpProtocolsFeature(selectedProtocol));
                    break;
                }
            }
        }
        finally
        {
            selectionState.Complete();
            shutdownRegistration.Dispose();
            var timerCallbacks = selectionState.DetachTimer();
            timeoutTimer?.Dispose();
            await timerCallbacks.ConfigureAwait(false);
            selectionState.Dispose();
        }

        // Do not retain the selection state and disposed timer for the lifetime of the HTTP connection.
        selectionState = null!;
        timeoutTimer = null;
        shutdownRegistration = default;
        await _next(connectionContext);
    }

    private static async Task CompleteStopAsync(SelectionState.StopOperation stopOperation, ConnectionContext connectionContext)
    {
        var callbackException = await stopOperation.Completion.ConfigureAwait(false);
        callbackException?.Throw();

        if (stopOperation.TimedOut)
        {
            KestrelMetrics.AddConnectionEndReason(
                connectionContext.Features.Get<IConnectionMetricsTagsFeature>(),
                ConnectionEndReason.KeepAliveTimeout);
        }
    }

    private sealed class SelectionState
    {
        private static readonly object PendingState = new();
        private static readonly object SelectedState = new();
        private static readonly object CompletedState = new();
        private static readonly TimeSpan MaxTimerDuration = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

        private readonly PipeReader _input;
        private readonly ConnectionContext _connectionContext;
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _timeout;
        private readonly IDebugger _debugger;
        private readonly long _startTimestamp;
        private readonly CancellationTokenSource _readCancellation = new();
        private readonly TimerState _timerState;
        private ITimer? _timer;
        private int _timerCallbackBeforePublication;
        private object _state = PendingState;

        public SelectionState(
            PipeReader input,
            ConnectionContext connectionContext,
            TimeProvider timeProvider,
            TimeSpan timeout,
            IDebugger debugger)
        {
            _input = input;
            _connectionContext = connectionContext;
            _timeProvider = timeProvider;
            _timeout = timeout;
            _debugger = debugger;
            _startTimestamp = timeProvider.GetTimestamp();
            _timerState = new TimerState(this);
        }

        public StopOperation? GetStopOperation() => Volatile.Read(ref _state) as StopOperation;

        public CancellationToken ReadCancellationToken => _readCancellation.Token;

        public void Dispose() => _readCancellation.Dispose();

        public Task DetachTimer() => _timerState.DetachAndWaitForCallbacksAsync();

        public ITimer? StartTimeoutTimer()
        {
            if (_timeout == TimeSpan.MaxValue)
            {
                return null;
            }

            var timer = _timeProvider.CreateTimer(
                static state => ((TimerState)state!).Invoke(),
                _timerState,
                GetTimerDuration(_timeout),
                Timeout.InfiniteTimeSpan);
            Interlocked.Exchange(ref _timer, timer);
            if (Interlocked.Exchange(ref _timerCallbackBeforePublication, 0) != 0)
            {
                _timerState.Invoke();
            }
            return timer;
        }

        public bool TrySelect()
            => ReferenceEquals(Interlocked.CompareExchange(ref _state, SelectedState, PendingState), PendingState);

        public bool TryComplete()
            => ReferenceEquals(Interlocked.CompareExchange(ref _state, CompletedState, PendingState), PendingState);

        public void Complete() => TryComplete();

        public void RequestShutdown() => TryStop(timedOut: false, callbackException: null);

        private void OnTimeoutTimer()
        {
            try
            {
                OnTimeoutTimerCore();
            }
            catch (Exception ex)
            {
                TryStop(timedOut: true, ExceptionDispatchInfo.Capture(ex));
            }
        }

        private void OnTimeoutTimerCore()
        {
            if (!ReferenceEquals(Volatile.Read(ref _state), PendingState))
            {
                return;
            }

            if (_debugger.IsAttached)
            {
                RearmTimer(Heartbeat.Interval);
                return;
            }

            var remaining = _timeout - _timeProvider.GetElapsedTime(_startTimestamp);
            if (remaining > TimeSpan.Zero)
            {
                RearmTimer(remaining);
                return;
            }

            TryStop(timedOut: true, callbackException: null);
        }

        private void RearmTimer(TimeSpan duration)
        {
            var timer = Volatile.Read(ref _timer);
            if (timer is null)
            {
                Interlocked.Exchange(ref _timerCallbackBeforePublication, 1);
                timer = Volatile.Read(ref _timer);
                if (timer is null)
                {
                    return;
                }
                Interlocked.Exchange(ref _timerCallbackBeforePublication, 0);
            }

            timer.Change(GetTimerDuration(duration), Timeout.InfiniteTimeSpan);
        }

        private void TryStop(bool timedOut, ExceptionDispatchInfo? callbackException)
        {
            var stopOperation = new StopOperation(timedOut);
            if (!ReferenceEquals(Interlocked.CompareExchange(ref _state, stopOperation, PendingState), PendingState))
            {
                return;
            }

            try
            {
                _input.CancelPendingRead();
            }
            catch (Exception ex)
            {
                callbackException ??= ExceptionDispatchInfo.Capture(ex);
                try
                {
                    _readCancellation.Cancel();
                }
                catch
                {
                    // The CancelPendingRead exception remains the callback failure surfaced by the middleware task.
                }

                try
                {
                    _connectionContext.Abort(new ConnectionAbortedException("Failed to cancel HTTP/2 preface detection.", ex));
                }
                catch
                {
                    // The callback exception is surfaced by the middleware task after the stop operation completes.
                }
            }
            finally
            {
                stopOperation.Complete(callbackException);
            }
        }

        private static TimeSpan GetTimerDuration(TimeSpan duration)
            => duration <= MaxTimerDuration ? duration : MaxTimerDuration;

        private sealed class TimerState
        {
            private SelectionState? _owner;
            private TaskCompletionSource? _callbacksCompleted;
            private int _activeCallbacks;

            public TimerState(SelectionState owner)
            {
                _owner = owner;
            }

            public void Invoke()
            {
                Interlocked.Increment(ref _activeCallbacks);
                try
                {
                    Volatile.Read(ref _owner)?.OnTimeoutTimer();
                }
                finally
                {
                    if (Interlocked.Decrement(ref _activeCallbacks) == 0)
                    {
                        Volatile.Read(ref _callbacksCompleted)?.TrySetResult();
                    }
                }
            }

            public Task DetachAndWaitForCallbacksAsync()
            {
                Interlocked.Exchange(ref _owner, null);
                if (Volatile.Read(ref _activeCallbacks) == 0)
                {
                    return Task.CompletedTask;
                }

                var newCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var completion = Interlocked.CompareExchange(ref _callbacksCompleted, newCompletion, null) ?? newCompletion;
                if (Volatile.Read(ref _activeCallbacks) == 0)
                {
                    completion.TrySetResult();
                }

                return completion.Task;
            }
        }

        public sealed class StopOperation
        {
            private readonly TaskCompletionSource<ExceptionDispatchInfo?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public StopOperation(bool timedOut)
            {
                TimedOut = timedOut;
            }

            public Task<ExceptionDispatchInfo?> Completion => _completion.Task;

            public bool TimedOut { get; }

            public void Complete(ExceptionDispatchInfo? callbackException) => _completion.TrySetResult(callbackException);
        }
    }
}
