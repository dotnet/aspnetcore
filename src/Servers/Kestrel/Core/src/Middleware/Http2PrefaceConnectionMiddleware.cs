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

    public Http2PrefaceConnectionMiddleware(ConnectionDelegate next, ServiceContext serviceContext, HttpProtocols endpointDefaultProtocols)
    {
        _next = next;
        _endpointDefaultProtocols = endpointDefaultProtocols;
        _timeProvider = serviceContext.TimeProvider;
        _keepAliveTimeout = serviceContext.ServerOptions.Limits.KeepAliveTimeout;
        _log = serviceContext.Log;
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
        var selectionState = new SelectionState(input, connectionContext, _timeProvider, _keepAliveTimeout);
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
                    result = await input.ReadAsync();
                }
                catch (ConnectionResetException)
                {
                    selectionState.Complete();
                    selectionState.ThrowCallbackException();
                    return;
                }
                catch (IOException ex)
                {
                    selectionState.Complete();
                    selectionState.ThrowCallbackException();
                    _log.RequestProcessingError(connectionContext.ConnectionId, ex);
                    return;
                }
                catch (ConnectionAbortedException ex)
                {
                    selectionState.Complete();
                    selectionState.ThrowCallbackException();
                    _log.RequestProcessingError(connectionContext.ConnectionId, ex);
                    return;
                }
                catch
                {
                    selectionState.Complete();
                    selectionState.ThrowCallbackException();
                    throw;
                }

                var buffer = result.Buffer;
                var examined = buffer.Start;

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
                        selectionState.Complete();
                        return;
                    }
                }
                finally
                {
                    input.AdvanceTo(buffer.Start, examined);
                }

                if (selectionState.StopRequested)
                {
                    selectionState.ThrowCallbackException();
                    if (selectionState.TimedOut)
                    {
                        KestrelMetrics.AddConnectionEndReason(
                            connectionContext.Features.Get<IConnectionMetricsTagsFeature>(),
                            ConnectionEndReason.KeepAliveTimeout);
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
            timeoutTimer?.Dispose();
            shutdownRegistration.Dispose();
        }

        await _next(connectionContext);
    }

    private sealed class SelectionState
    {
        private const int PendingState = 0;
        private const int SelectedState = 1;
        private const int ShutdownState = 2;
        private const int TimedOutState = 3;
        private const int CompletedState = 4;
        private static readonly TimeSpan MaxTimerDuration = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

        private readonly PipeReader _input;
        private readonly ConnectionContext _connectionContext;
        private readonly TimeProvider _timeProvider;
        private readonly TimeSpan _timeout;
        private readonly long _startTimestamp;
        private ITimer? _timer;
        private ExceptionDispatchInfo? _callbackException;
        private int _timerCallbackBeforePublication;
        private int _state;

        public SelectionState(PipeReader input, ConnectionContext connectionContext, TimeProvider timeProvider, TimeSpan timeout)
        {
            _input = input;
            _connectionContext = connectionContext;
            _timeProvider = timeProvider;
            _timeout = timeout;
            _startTimestamp = timeProvider.GetTimestamp();
        }

        public bool StopRequested
        {
            get
            {
                var state = Volatile.Read(ref _state);
                return state is ShutdownState or TimedOutState;
            }
        }

        public bool TimedOut => Volatile.Read(ref _state) == TimedOutState;

        public ITimer StartTimeoutTimer()
        {
            var timer = _timeProvider.CreateTimer(
                static state => ((SelectionState)state!).OnTimeoutTimer(),
                this,
                GetTimerDuration(_timeout),
                Timeout.InfiniteTimeSpan);
            Volatile.Write(ref _timer, timer);
            if (Interlocked.Exchange(ref _timerCallbackBeforePublication, 0) != 0)
            {
                OnTimeoutTimer();
            }
            return timer;
        }

        public bool TrySelect() => Interlocked.CompareExchange(ref _state, SelectedState, PendingState) == PendingState;

        public void Complete() => Interlocked.CompareExchange(ref _state, CompletedState, PendingState);

        public void RequestShutdown()
        {
            if (Interlocked.CompareExchange(ref _state, ShutdownState, PendingState) == PendingState)
            {
                CancelPendingRead();
            }
        }

        public void ThrowCallbackException() => Volatile.Read(ref _callbackException)?.Throw();

        private void OnTimeoutTimer()
        {
            try
            {
                OnTimeoutTimerCore();
            }
            catch (Exception ex)
            {
                if (Interlocked.CompareExchange(ref _state, TimedOutState, PendingState) == PendingState)
                {
                    Interlocked.CompareExchange(ref _callbackException, ExceptionDispatchInfo.Capture(ex), null);
                    CancelPendingRead();
                }
            }
        }

        private void OnTimeoutTimerCore()
        {
            if (Volatile.Read(ref _state) != PendingState)
            {
                return;
            }

            var remaining = _timeout - _timeProvider.GetElapsedTime(_startTimestamp);
            if (remaining > TimeSpan.Zero)
            {
                var timer = Volatile.Read(ref _timer);
                if (timer is null)
                {
                    Volatile.Write(ref _timerCallbackBeforePublication, 1);
                    timer = Volatile.Read(ref _timer);
                    if (timer is null)
                    {
                        return;
                    }
                    Interlocked.Exchange(ref _timerCallbackBeforePublication, 0);
                }

                timer.Change(GetTimerDuration(remaining), Timeout.InfiniteTimeSpan);
                return;
            }

            if (Interlocked.CompareExchange(ref _state, TimedOutState, PendingState) == PendingState)
            {
                CancelPendingRead();
            }
        }

        private void CancelPendingRead()
        {
            try
            {
                _input.CancelPendingRead();
            }
            catch (Exception ex)
            {
                Interlocked.CompareExchange(ref _callbackException, ExceptionDispatchInfo.Capture(ex), null);
                try
                {
                    _connectionContext.Abort(new ConnectionAbortedException("Failed to cancel HTTP/2 preface detection.", ex));
                }
                catch
                {
                    // The original callback exception is surfaced by the middleware task when the read completes.
                }
            }
        }

        private static TimeSpan GetTimerDuration(TimeSpan duration)
            => duration <= MaxTimerDuration ? duration : MaxTimerDuration;
    }
}
