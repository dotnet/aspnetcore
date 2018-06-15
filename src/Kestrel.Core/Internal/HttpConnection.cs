// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class HttpConnection : ITimeoutControl, IConnectionTimeoutFeature
    {
        private static readonly ReadOnlyMemory<byte> Http2Id = new[] { (byte)'h', (byte)'2' };

        private readonly HttpConnectionContext _context;
        private readonly TaskCompletionSource<object> _socketClosedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<object> _lifetimeTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        private IList<IAdaptedConnection> _adaptedConnections;
        private IDuplexPipe _adaptedTransport;

        private readonly object _protocolSelectionLock = new object();
        private ProtocolSelectionState _protocolSelectionState = ProtocolSelectionState.Initializing;
        private IRequestProcessor _requestProcessor;
        private Http1Connection _http1Connection;

        private long _lastTimestamp;
        private long _timeoutTimestamp = long.MaxValue;
        private TimeoutAction _timeoutAction;

        private readonly object _readTimingLock = new object();
        private bool _readTimingEnabled;
        private bool _readTimingPauseRequested;
        private long _readTimingElapsedTicks;
        private long _readTimingBytesRead;

        private readonly object _writeTimingLock = new object();
        private int _writeTimingWrites;
        private long _writeTimingTimeoutTimestamp;

        public HttpConnection(HttpConnectionContext context)
        {
            _context = context;
        }

        // For testing
        internal HttpProtocol Http1Connection => _http1Connection;
        internal IDebugger Debugger { get; set; } = DebuggerWrapper.Singleton;

        // For testing
        internal bool RequestTimedOut { get; private set; }

        public string ConnectionId => _context.ConnectionId;
        public IPEndPoint LocalEndPoint => _context.LocalEndPoint;
        public IPEndPoint RemoteEndPoint => _context.RemoteEndPoint;

        private MemoryPool<byte> MemoryPool => _context.MemoryPool;

        // Internal for testing
        internal PipeOptions AdaptedInputPipeOptions => new PipeOptions
        (
            pool: MemoryPool,
            readerScheduler: _context.ServiceContext.Scheduler,
            writerScheduler: PipeScheduler.Inline,
            pauseWriterThreshold: _context.ServiceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            resumeWriterThreshold: _context.ServiceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            useSynchronizationContext: false,
            minimumSegmentSize: KestrelMemoryPool.MinimumSegmentSize
        );

        internal PipeOptions AdaptedOutputPipeOptions => new PipeOptions
        (
            pool: MemoryPool,
            readerScheduler: PipeScheduler.Inline,
            writerScheduler: PipeScheduler.Inline,
            pauseWriterThreshold: _context.ServiceContext.ServerOptions.Limits.MaxResponseBufferSize ?? 0,
            resumeWriterThreshold: _context.ServiceContext.ServerOptions.Limits.MaxResponseBufferSize ?? 0,
            useSynchronizationContext: false,
            minimumSegmentSize: KestrelMemoryPool.MinimumSegmentSize
        );

        private IKestrelTrace Log => _context.ServiceContext.Log;

        public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> httpApplication)
        {
            try
            {
                // TODO: When we start tracking all connection middleware for shutdown, go back
                // to logging connections tart and stop in ConnectionDispatcher so we get these
                // logs for all connection middleware.
                Log.ConnectionStart(ConnectionId);
                KestrelEventSource.Log.ConnectionStart(this);

                AdaptedPipeline adaptedPipeline = null;
                var adaptedPipelineTask = Task.CompletedTask;

                // _adaptedTransport must be set prior to adding the connection to the manager in order
                // to allow the connection to be aported prior to protocol selection.
                _adaptedTransport = _context.Transport;
                var application = _context.Application;


                if (_context.ConnectionAdapters.Count > 0)
                {
                    adaptedPipeline = new AdaptedPipeline(_adaptedTransport,
                                                          new Pipe(AdaptedInputPipeOptions),
                                                          new Pipe(AdaptedOutputPipeOptions),
                                                          Log);

                    _adaptedTransport = adaptedPipeline;
                }

                _lastTimestamp = _context.ServiceContext.SystemClock.UtcNow.Ticks;

                _context.ConnectionFeatures.Set<IConnectionTimeoutFeature>(this);

                if (adaptedPipeline != null)
                {
                    // Stream can be null here and run async will close the connection in that case
                    var stream = await ApplyConnectionAdaptersAsync();
                    adaptedPipelineTask = adaptedPipeline.RunAsync(stream);
                }

                IRequestProcessor requestProcessor = null;

                lock (_protocolSelectionLock)
                {
                    // Ensure that the connection hasn't already been stopped.
                    if (_protocolSelectionState == ProtocolSelectionState.Initializing)
                    {
                        switch (SelectProtocol())
                        {
                            case HttpProtocols.Http1:
                                // _http1Connection must be initialized before adding the connection to the connection manager
                                requestProcessor = _http1Connection = CreateHttp1Connection(_adaptedTransport, application);
                                _protocolSelectionState = ProtocolSelectionState.Selected;
                                break;
                            case HttpProtocols.Http2:
                                // _http2Connection must be initialized before yielding control to the transport thread,
                                // to prevent a race condition where _http2Connection.Abort() is called just as
                                // _http2Connection is about to be initialized.
                                requestProcessor = CreateHttp2Connection(_adaptedTransport, application);
                                _protocolSelectionState = ProtocolSelectionState.Selected;
                                break;
                            case HttpProtocols.None:
                                // An error was already logged in SelectProtocol(), but we should close the connection.
                                Abort(ex: null);
                                break;
                            default:
                                // SelectProtocol() only returns Http1, Http2 or None.
                                throw new NotSupportedException($"{nameof(SelectProtocol)} returned something other than Http1, Http2 or None.");
                        }

                        _requestProcessor = requestProcessor;
                    }
                }

                if (requestProcessor != null)
                {
                    await requestProcessor.ProcessRequestsAsync(httpApplication);
                }

                await adaptedPipelineTask;
                await _socketClosedTcs.Task;
            }
            catch (Exception ex)
            {
                Log.LogCritical(0, ex, $"Unexpected exception in {nameof(HttpConnection)}.{nameof(ProcessRequestsAsync)}.");
            }
            finally
            {
                DisposeAdaptedConnections();

                if (_http1Connection?.IsUpgraded == true)
                {
                    _context.ServiceContext.ConnectionManager.UpgradedConnectionCount.ReleaseOne();
                }

                Log.ConnectionStop(ConnectionId);
                KestrelEventSource.Log.ConnectionStop(this);

                _lifetimeTcs.SetResult(null);
            }
        }

        // For testing only
        internal void Initialize(IDuplexPipe transport, IDuplexPipe application)
        {
            _requestProcessor = _http1Connection = CreateHttp1Connection(transport, application);
            _protocolSelectionState = ProtocolSelectionState.Selected;
        }

        private Http1Connection CreateHttp1Connection(IDuplexPipe transport, IDuplexPipe application)
        {
            return new Http1Connection(new Http1ConnectionContext
            {
                ConnectionId = _context.ConnectionId,
                ConnectionFeatures = _context.ConnectionFeatures,
                MemoryPool = MemoryPool,
                LocalEndPoint = LocalEndPoint,
                RemoteEndPoint = RemoteEndPoint,
                ServiceContext = _context.ServiceContext,
                ConnectionContext = _context.ConnectionContext,
                TimeoutControl = this,
                Transport = transport,
                Application = application
            });
        }

        private Http2Connection CreateHttp2Connection(IDuplexPipe transport, IDuplexPipe application)
        {
            return new Http2Connection(new Http2ConnectionContext
            {
                ConnectionId = _context.ConnectionId,
                ServiceContext = _context.ServiceContext,
                ConnectionFeatures = _context.ConnectionFeatures,
                MemoryPool = MemoryPool,
                LocalEndPoint = LocalEndPoint,
                RemoteEndPoint = RemoteEndPoint,
                Application = application,
                Transport = transport
            });
        }

        public void OnConnectionClosed()
        {
            _socketClosedTcs.TrySetResult(null);
        }

        public Task StopProcessingNextRequestAsync()
        {
            lock (_protocolSelectionLock)
            {
                switch (_protocolSelectionState)
                {
                    case ProtocolSelectionState.Initializing:
                        CloseUninitializedConnection(abortReason: null);
                        _protocolSelectionState = ProtocolSelectionState.Aborted;
                        break;
                    case ProtocolSelectionState.Selected:
                        _requestProcessor.StopProcessingNextRequest();
                        break;
                    case ProtocolSelectionState.Aborted:
                        break;
                }
            }

            return _lifetimeTcs.Task;
        }

        public void OnInputOrOutputCompleted()
        {
            lock (_protocolSelectionLock)
            {
                switch (_protocolSelectionState)
                {
                    case ProtocolSelectionState.Initializing:
                        CloseUninitializedConnection(abortReason: null);
                        _protocolSelectionState = ProtocolSelectionState.Aborted;
                        break;
                    case ProtocolSelectionState.Selected:
                        _requestProcessor.OnInputOrOutputCompleted();
                        break;
                    case ProtocolSelectionState.Aborted:
                        break;
                }

            }
        }

        public void Abort(ConnectionAbortedException ex)
        {
            lock (_protocolSelectionLock)
            {
                switch (_protocolSelectionState)
                {
                    case ProtocolSelectionState.Initializing:
                        CloseUninitializedConnection(ex);
                        break;
                    case ProtocolSelectionState.Selected:
                        _requestProcessor.Abort(ex);
                        break;
                    case ProtocolSelectionState.Aborted:
                        break;
                }

                _protocolSelectionState = ProtocolSelectionState.Aborted;
            }
        }

        public Task AbortAsync(ConnectionAbortedException ex)
        {
            Abort(ex);

            return _socketClosedTcs.Task;
        }

        private async Task<Stream> ApplyConnectionAdaptersAsync()
        {
            var connectionAdapters = _context.ConnectionAdapters;
            var stream = new RawStream(_context.Transport.Input, _context.Transport.Output);
            var adapterContext = new ConnectionAdapterContext(_context.ConnectionContext, stream);
            _adaptedConnections = new List<IAdaptedConnection>(connectionAdapters.Count);

            try
            {
                for (var i = 0; i < connectionAdapters.Count; i++)
                {
                    var adaptedConnection = await connectionAdapters[i].OnConnectionAsync(adapterContext);
                    _adaptedConnections.Add(adaptedConnection);
                    adapterContext = new ConnectionAdapterContext(_context.ConnectionContext, adaptedConnection.ConnectionStream);
                }
            }
            catch (Exception ex)
            {
                Log.LogError(0, ex, $"Uncaught exception from the {nameof(IConnectionAdapter.OnConnectionAsync)} method of an {nameof(IConnectionAdapter)}.");

                return null;
            }

            return adapterContext.ConnectionStream;
        }

        private void DisposeAdaptedConnections()
        {
            var adaptedConnections = _adaptedConnections;
            if (adaptedConnections != null)
            {
                for (var i = adaptedConnections.Count - 1; i >= 0; i--)
                {
                    adaptedConnections[i].Dispose();
                }
            }
        }

        private HttpProtocols SelectProtocol()
        {
            var hasTls = _context.ConnectionFeatures.Get<ITlsConnectionFeature>() != null;
            var applicationProtocol = _context.ConnectionFeatures.Get<ITlsApplicationProtocolFeature>()?.ApplicationProtocol
                ?? new ReadOnlyMemory<byte>();
            var http1Enabled = (_context.Protocols & HttpProtocols.Http1) == HttpProtocols.Http1;
            var http2Enabled = (_context.Protocols & HttpProtocols.Http2) == HttpProtocols.Http2;

            string error = null;

            if (_context.Protocols == HttpProtocols.None)
            {
                error = CoreStrings.EndPointRequiresAtLeastOneProtocol;
            }

            if (!hasTls && http1Enabled && http2Enabled)
            {
                error = CoreStrings.EndPointRequiresTlsForHttp1AndHttp2;
            }

            if (!http1Enabled && http2Enabled && hasTls && !Http2Id.Span.SequenceEqual(applicationProtocol.Span))
            {
                error = CoreStrings.EndPointHttp2NotNegotiated;
            }

            if (error != null)
            {
                Log.LogError(0, error);
                return HttpProtocols.None;
            }

            return http2Enabled && (!hasTls || Http2Id.Span.SequenceEqual(applicationProtocol.Span)) ? HttpProtocols.Http2 : HttpProtocols.Http1;
        }

        public void Tick(DateTimeOffset now)
        {
            if (_protocolSelectionState == ProtocolSelectionState.Aborted)
            {
                // It's safe to check for timeouts on a dead connection,
                // but try not to in order to avoid extraneous logs.
                return;
            }

            var timestamp = now.Ticks;

            CheckForTimeout(timestamp);

            // HTTP/2 rate timeouts are not yet supported.
            if (_http1Connection != null)
            {
                CheckForReadDataRateTimeout(timestamp);
                CheckForWriteDataRateTimeout(timestamp);
            }

            Interlocked.Exchange(ref _lastTimestamp, timestamp);
        }

        private void CheckForTimeout(long timestamp)
        {
            // TODO: Use PlatformApis.VolatileRead equivalent again
            if (timestamp > Interlocked.Read(ref _timeoutTimestamp))
            {
                if (!Debugger.IsAttached)
                {
                    CancelTimeout();

                    switch (_timeoutAction)
                    {
                        case TimeoutAction.StopProcessingNextRequest:
                            // Http/2 keep-alive timeouts are not yet supported.
                            _http1Connection?.StopProcessingNextRequest();
                            break;
                        case TimeoutAction.SendTimeoutResponse:
                            // HTTP/2 timeout responses are not yet supported.
                            if (_http1Connection != null)
                            {
                                RequestTimedOut = true;
                                _http1Connection.SendTimeoutResponse();
                            }
                            break;
                        case TimeoutAction.AbortConnection:
                            // This is actually supported with HTTP/2!
                            Abort(new ConnectionAbortedException(CoreStrings.ConnectionTimedOutByServer));
                            break;
                    }
                }
            }
        }

        private void CheckForReadDataRateTimeout(long timestamp)
        {
            Debug.Assert(_http1Connection != null);

            // The only time when both a timeout is set and the read data rate could be enforced is
            // when draining the request body. Since there's already a (short) timeout set for draining,
            // it's safe to not check the data rate at this point.
            if (Interlocked.Read(ref _timeoutTimestamp) != long.MaxValue)
            {
                return;
            }

            lock (_readTimingLock)
            {
                if (_readTimingEnabled)
                {
                    // Reference in local var to avoid torn reads in case the min rate is changed via IHttpMinRequestBodyDataRateFeature
                    var minRequestBodyDataRate = _http1Connection.MinRequestBodyDataRate;

                    _readTimingElapsedTicks += timestamp - _lastTimestamp;

                    if (minRequestBodyDataRate?.BytesPerSecond > 0 && _readTimingElapsedTicks > minRequestBodyDataRate.GracePeriod.Ticks)
                    {
                        var elapsedSeconds = (double)_readTimingElapsedTicks / TimeSpan.TicksPerSecond;
                        var rate = Interlocked.Read(ref _readTimingBytesRead) / elapsedSeconds;

                        if (rate < minRequestBodyDataRate.BytesPerSecond && !Debugger.IsAttached)
                        {
                            Log.RequestBodyMininumDataRateNotSatisfied(_context.ConnectionId, _http1Connection.TraceIdentifier, minRequestBodyDataRate.BytesPerSecond);
                            RequestTimedOut = true;
                            _http1Connection.SendTimeoutResponse();
                        }
                    }

                    // PauseTimingReads() cannot just set _timingReads to false. It needs to go through at least one tick
                    // before pausing, otherwise _readTimingElapsed might never be updated if PauseTimingReads() is always
                    // called before the next tick.
                    if (_readTimingPauseRequested)
                    {
                        _readTimingEnabled = false;
                        _readTimingPauseRequested = false;
                    }
                }
            }
        }

        private void CheckForWriteDataRateTimeout(long timestamp)
        {
            Debug.Assert(_http1Connection != null);

            lock (_writeTimingLock)
            {
                if (_writeTimingWrites > 0 && timestamp > _writeTimingTimeoutTimestamp && !Debugger.IsAttached)
                {
                    RequestTimedOut = true;
                    Log.ResponseMininumDataRateNotSatisfied(_http1Connection.ConnectionIdFeature, _http1Connection.TraceIdentifier);
                    Abort(new ConnectionAbortedException(CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied));
                }
            }
        }

        public void SetTimeout(long ticks, TimeoutAction timeoutAction)
        {
            Debug.Assert(_timeoutTimestamp == long.MaxValue, "Concurrent timeouts are not supported");

            AssignTimeout(ticks, timeoutAction);
        }

        public void ResetTimeout(long ticks, TimeoutAction timeoutAction)
        {
            AssignTimeout(ticks, timeoutAction);
        }

        public void CancelTimeout()
        {
            Interlocked.Exchange(ref _timeoutTimestamp, long.MaxValue);
        }

        private void AssignTimeout(long ticks, TimeoutAction timeoutAction)
        {
            _timeoutAction = timeoutAction;

            // Add Heartbeat.Interval since this can be called right before the next heartbeat.
            Interlocked.Exchange(ref _timeoutTimestamp, _lastTimestamp + ticks + Heartbeat.Interval.Ticks);
        }

        public void StartTimingReads()
        {
            lock (_readTimingLock)
            {
                _readTimingElapsedTicks = 0;
                _readTimingBytesRead = 0;
                _readTimingEnabled = true;
            }
        }

        public void StopTimingReads()
        {
            lock (_readTimingLock)
            {
                _readTimingEnabled = false;
            }
        }

        public void PauseTimingReads()
        {
            lock (_readTimingLock)
            {
                _readTimingPauseRequested = true;
            }
        }

        public void ResumeTimingReads()
        {
            lock (_readTimingLock)
            {
                _readTimingEnabled = true;

                // In case pause and resume were both called between ticks
                _readTimingPauseRequested = false;
            }
        }

        public void BytesRead(long count)
        {
            Interlocked.Add(ref _readTimingBytesRead, count);
        }

        public void StartTimingWrite(long size)
        {
            Debug.Assert(_http1Connection != null);

            lock (_writeTimingLock)
            {
                var minResponseDataRate = _http1Connection.MinResponseDataRate;

                if (minResponseDataRate != null)
                {
                    // Add Heartbeat.Interval since this can be called right before the next heartbeat.
                    var currentTimeUpperBound = _lastTimestamp + Heartbeat.Interval.Ticks;
                    var ticksToCompleteWriteAtMinRate = TimeSpan.FromSeconds(size / minResponseDataRate.BytesPerSecond).Ticks;

                    // If ticksToCompleteWriteAtMinRate is less than the configured grace period,
                    // allow that write to take up to the grace period to complete. Only add the grace period
                    // to the current time and not to any accumulated timeout.
                    var singleWriteTimeoutTimestamp = currentTimeUpperBound + Math.Max(
                        minResponseDataRate.GracePeriod.Ticks,
                        ticksToCompleteWriteAtMinRate);

                    // Don't penalize a connection for completing previous writes more quickly than required.
                    // We don't want to kill a connection when flushing the chunk terminator just because the previous
                    // chunk was large if the previous chunk was flushed quickly.

                    // Don't add any grace period to this accumulated timeout because the grace period could
                    // get accumulated repeatedly making the timeout for a bunch of consecutive small writes
                    // far too conservative.
                    var accumulatedWriteTimeoutTimestamp = _writeTimingTimeoutTimestamp + ticksToCompleteWriteAtMinRate;

                    _writeTimingTimeoutTimestamp = Math.Max(singleWriteTimeoutTimestamp, accumulatedWriteTimeoutTimestamp);
                    _writeTimingWrites++;
                }
            }
        }

        public void StopTimingWrite()
        {
            lock (_writeTimingLock)
            {
                _writeTimingWrites--;
            }
        }

        void IConnectionTimeoutFeature.SetTimeout(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentException(CoreStrings.PositiveFiniteTimeSpanRequired, nameof(timeSpan));
            }
            if (_timeoutTimestamp != long.MaxValue)
            {
                throw new InvalidOperationException(CoreStrings.ConcurrentTimeoutsNotSupported);
            }

            SetTimeout(timeSpan.Ticks, TimeoutAction.AbortConnection);
        }

        void IConnectionTimeoutFeature.ResetTimeout(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
            {
                throw new ArgumentException(CoreStrings.PositiveFiniteTimeSpanRequired, nameof(timeSpan));
            }

            ResetTimeout(timeSpan.Ticks, TimeoutAction.AbortConnection);
        }

        private void CloseUninitializedConnection(ConnectionAbortedException abortReason)
        {
            Debug.Assert(_adaptedTransport != null);

            _context.ConnectionContext.Abort(abortReason);

            _adaptedTransport.Input.Complete();
            _adaptedTransport.Output.Complete();
        }

        private enum ProtocolSelectionState
        {
            Initializing,
            Selected,
            Aborted
        }
    }
}
