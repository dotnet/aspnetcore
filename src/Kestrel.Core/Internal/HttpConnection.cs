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
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class HttpConnection : ITimeoutControl, IConnectionTimeoutFeature
    {
        private const int Http2ConnectionNotStarted = 0;
        private const int Http2ConnectionStarted = 1;
        private const int Http2ConnectionClosed = 2;

        private readonly HttpConnectionContext _context;
        private IList<IAdaptedConnection> _adaptedConnections;
        private readonly TaskCompletionSource<object> _socketClosedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Http1Connection _http1Connection;
        private Http2Connection _http2Connection;
        private volatile int _http2ConnectionState;

        private long _lastTimestamp;
        private long _timeoutTimestamp = long.MaxValue;
        private TimeoutAction _timeoutAction;

        private object _readTimingLock = new object();
        private bool _readTimingEnabled;
        private bool _readTimingPauseRequested;
        private long _readTimingElapsedTicks;
        private long _readTimingBytesRead;

        private object _writeTimingLock = new object();
        private int _writeTimingWrites;
        private long _writeTimingTimeoutTimestamp;

        private Task _lifetimeTask;

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

        private BufferPool BufferPool => _context.BufferPool;

        // Internal for testing
        internal PipeOptions AdaptedInputPipeOptions => new PipeOptions
        (
            bufferPool: BufferPool,
            readerScheduler: _context.ServiceContext.ThreadPool,
            writerScheduler: InlineScheduler.Default,
            maximumSizeHigh: _context.ServiceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            maximumSizeLow: _context.ServiceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0
        );

        internal PipeOptions AdaptedOutputPipeOptions => new PipeOptions
        (
            bufferPool: BufferPool,
            readerScheduler: InlineScheduler.Default,
            writerScheduler: InlineScheduler.Default,
            maximumSizeHigh: _context.ServiceContext.ServerOptions.Limits.MaxResponseBufferSize ?? 0,
            maximumSizeLow: _context.ServiceContext.ServerOptions.Limits.MaxResponseBufferSize ?? 0
        );

        private IKestrelTrace Log => _context.ServiceContext.Log;

        public Task StartRequestProcessing<TContext>(IHttpApplication<TContext> application)
        {
            return _lifetimeTask = ProcessRequestsAsync(application);
        }

        private async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> httpApplication)
        {
            try
            {
                KestrelEventSource.Log.ConnectionStart(this);

                AdaptedPipeline adaptedPipeline = null;
                var adaptedPipelineTask = Task.CompletedTask;
                var transport = _context.Transport;
                var application = _context.Application;


                if (_context.ConnectionAdapters.Count > 0)
                {
                    adaptedPipeline = new AdaptedPipeline(transport,
                                                          application,
                                                          new Pipe(AdaptedInputPipeOptions),
                                                          new Pipe(AdaptedOutputPipeOptions));

                    transport = adaptedPipeline;
                }

                // _http1Connection must be initialized before adding the connection to the connection manager
                CreateHttp1Connection(httpApplication, transport, application);

                // _http2Connection must be initialized before yielding control to the transport thread,
                // to prevent a race condition where _http2Connection.Abort() is called just as
                // _http2Connection is about to be initialized.
                CreateHttp2Connection(httpApplication, transport, application);

                // Do this before the first await so we don't yield control to the transport until we've
                // added the connection to the connection manager
                _context.ServiceContext.ConnectionManager.AddConnection(_context.HttpConnectionId, this);
                _lastTimestamp = _context.ServiceContext.SystemClock.UtcNow.Ticks;

                _http1Connection.ConnectionFeatures.Set<IConnectionTimeoutFeature>(this);
                _http2Connection.ConnectionFeatures.Set<IConnectionTimeoutFeature>(this);

                if (adaptedPipeline != null)
                {
                    // Stream can be null here and run async will close the connection in that case
                    var stream = await ApplyConnectionAdaptersAsync();
                    adaptedPipelineTask = adaptedPipeline.RunAsync(stream);
                }

                var protocol = SelectProtocol();

                if (protocol == HttpProtocols.None)
                {
                    Abort(ex: null);
                }

                // One of these has to run even if no protocol was selected so the abort propagates and everything completes properly
                if (protocol == HttpProtocols.Http2 && Interlocked.CompareExchange(ref _http2ConnectionState, Http2ConnectionStarted, Http2ConnectionNotStarted) == Http2ConnectionNotStarted)
                {
                    await _http2Connection.ProcessAsync(httpApplication);
                }
                else
                {
                    await _http1Connection.ProcessRequestsAsync();
                }

                await adaptedPipelineTask;
                await _socketClosedTcs.Task;
            }
            catch (Exception ex)
            {
                Log.LogError(0, ex, $"Unexpected exception in {nameof(HttpConnection)}.{nameof(ProcessRequestsAsync)}.");
            }
            finally
            {
                _context.ServiceContext.ConnectionManager.RemoveConnection(_context.HttpConnectionId);
                DisposeAdaptedConnections();

                if (_http1Connection.IsUpgraded)
                {
                    _context.ServiceContext.ConnectionManager.UpgradedConnectionCount.ReleaseOne();
                }

                KestrelEventSource.Log.ConnectionStop(this);
            }
        }

        internal void CreateHttp1Connection<TContext>(IHttpApplication<TContext> httpApplication, IPipeConnection transport, IPipeConnection application)
        {
            _http1Connection = new Http1Connection<TContext>(httpApplication, new Http1ConnectionContext
            {
                ConnectionId = _context.ConnectionId,
                ConnectionFeatures = _context.ConnectionFeatures,
                BufferPool = BufferPool,
                LocalEndPoint = LocalEndPoint,
                RemoteEndPoint = RemoteEndPoint,
                ServiceContext = _context.ServiceContext,
                TimeoutControl = this,
                Transport = transport,
                Application = application
            });
        }

        internal void CreateHttp2Connection<TContext>(IHttpApplication<TContext> httpApplication, IPipeConnection transport, IPipeConnection application)
        {
            _http2Connection = new Http2Connection(new Http2ConnectionContext
            {
                ConnectionId = _context.ConnectionId,
                ServiceContext = _context.ServiceContext,
                ConnectionFeatures = _context.ConnectionFeatures,
                BufferPool = BufferPool,
                LocalEndPoint = LocalEndPoint,
                RemoteEndPoint = RemoteEndPoint,
                Application = application,
                Transport = transport
            });
        }

        public void OnConnectionClosed(Exception ex)
        {
            Abort(ex);

            _socketClosedTcs.TrySetResult(null);
        }

        public Task StopProcessingNextRequestAsync()
        {
            Debug.Assert(_http1Connection != null, $"{nameof(_http1Connection)} is null");
            Debug.Assert(_http2Connection != null, $"{nameof(_http2Connection)} is null");

            if (Interlocked.Exchange(ref _http2ConnectionState, Http2ConnectionClosed) == Http2ConnectionStarted)
            {
                _http2Connection.Stop();
            }
            else
            {
                _http1Connection.StopProcessingNextRequest();
            }

            return _lifetimeTask;
        }

        public void Abort(Exception ex)
        {
            Debug.Assert(_http1Connection != null, $"{nameof(_http1Connection)} is null");
            Debug.Assert(_http2Connection != null, $"{nameof(_http2Connection)} is null");

            // Abort the connection (if not already aborted)
            if (Interlocked.Exchange(ref _http2ConnectionState, Http2ConnectionClosed) == Http2ConnectionStarted)
            {
                _http2Connection.Abort(ex);
            }
            else
            {
                _http1Connection.Abort(ex);
            }
        }

        public Task AbortAsync(Exception ex)
        {
            Abort(ex);

            return _lifetimeTask;
        }

        public void SendTimeoutResponse()
        {
            Debug.Assert(_http1Connection != null, $"{nameof(_http1Connection)} is null");
            Debug.Assert(_http2Connection != null, $"{nameof(_http2Connection)} is null");

            RequestTimedOut = true;
            _http1Connection.SendTimeoutResponse();
        }

        public void StopProcessingNextRequest()
        {
            Debug.Assert(_http1Connection != null, $"{nameof(_http1Connection)} is null");
            Debug.Assert(_http2Connection != null, $"{nameof(_http2Connection)} is null");

            _http1Connection.StopProcessingNextRequest();
        }

        private async Task<Stream> ApplyConnectionAdaptersAsync()
        {
            Debug.Assert(_http1Connection != null, $"{nameof(_http1Connection)} is null");
            Debug.Assert(_http2Connection != null, $"{nameof(_http2Connection)} is null");

            var connectionAdapters = _context.ConnectionAdapters;
            var stream = new RawStream(_context.Transport.Input, _context.Transport.Output);
            var adapterContext = new ConnectionAdapterContext(_context.ConnectionFeatures, stream);
            _adaptedConnections = new List<IAdaptedConnection>(connectionAdapters.Count);

            try
            {
                for (var i = 0; i < connectionAdapters.Count; i++)
                {
                    var adaptedConnection = await connectionAdapters[i].OnConnectionAsync(adapterContext);
                    _adaptedConnections.Add(adaptedConnection);
                    adapterContext = new ConnectionAdapterContext(_context.ConnectionFeatures, adaptedConnection.ConnectionStream);
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
            var applicationProtocol = _context.ConnectionFeatures.Get<ITlsApplicationProtocolFeature>()?.ApplicationProtocol;
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

            if (!http1Enabled && http2Enabled && hasTls && applicationProtocol != "h2")
            {
                error = CoreStrings.EndPointHttp2NotNegotiated;
            }

            if (error != null)
            {
                Log.LogError(0, error);
                return HttpProtocols.None;
            }

            return http2Enabled && (!hasTls || applicationProtocol == "h2") ? HttpProtocols.Http2 : HttpProtocols.Http1;
        }

        public void Tick(DateTimeOffset now)
        {
            Debug.Assert(_http1Connection != null, $"{nameof(_http1Connection)} is null");
            Debug.Assert(_http2Connection != null, $"{nameof(_http2Connection)} is null");

            var timestamp = now.Ticks;

            CheckForTimeout(timestamp);
            CheckForReadDataRateTimeout(timestamp);
            CheckForWriteDataRateTimeout(timestamp);

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
                            StopProcessingNextRequest();
                            break;
                        case TimeoutAction.SendTimeoutResponse:
                            SendTimeoutResponse();
                            break;
                        case TimeoutAction.AbortConnection:
                            Abort(new TimeoutException());
                            break;
                    }
                }
            }
        }

        private void CheckForReadDataRateTimeout(long timestamp)
        {
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
                            SendTimeoutResponse();
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
            lock (_writeTimingLock)
            {
                if (_writeTimingWrites > 0 && timestamp > _writeTimingTimeoutTimestamp && !Debugger.IsAttached)
                {
                    RequestTimedOut = true;
                    Log.ResponseMininumDataRateNotSatisfied(_http1Connection.ConnectionIdFeature, _http1Connection.TraceIdentifier);
                    Abort(new TimeoutException());
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
            lock (_writeTimingLock)
            {
                var minResponseDataRate = _http1Connection.MinResponseDataRate;

                if (minResponseDataRate != null)
                {
                    var timeoutTicks = Math.Max(
                        minResponseDataRate.GracePeriod.Ticks,
                        TimeSpan.FromSeconds(size / minResponseDataRate.BytesPerSecond).Ticks);

                    if (_writeTimingWrites == 0)
                    {
                        // Add Heartbeat.Interval since this can be called right before the next heartbeat.
                        _writeTimingTimeoutTimestamp = _lastTimestamp + Heartbeat.Interval.Ticks;
                    }

                    _writeTimingTimeoutTimestamp += timeoutTicks;
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
    }
}
