// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3Connection : IRequestProcessor, ITimeoutHandler
    {
        public DynamicTable DynamicTable { get; set; }

        public Http3ControlStream ControlStream { get; set; }
        public Http3ControlStream EncoderStream { get; set; }
        public Http3ControlStream DecoderStream { get; set; }

        internal readonly Dictionary<long, Http3Stream> _streams = new Dictionary<long, Http3Stream>();

        private long _highestOpenedStreamId; // TODO lock to access
        private volatile bool _haveSentGoAway;
        private object _sync = new object();
        private MultiplexedConnectionContext _multiplexedContext;
        private readonly Http3ConnectionContext _context;
        private readonly ISystemClock _systemClock;
        private readonly TimeoutControl _timeoutControl;
        private bool _aborted;
        private object _protocolSelectionLock = new object();

        public Http3Connection(Http3ConnectionContext context)
        {
            _multiplexedContext = context.ConnectionContext;
            _context = context;
            DynamicTable = new DynamicTable(0);
            _systemClock = context.ServiceContext.SystemClock;
            _timeoutControl = new TimeoutControl(this);
            _context.TimeoutControl ??= _timeoutControl;
        }

        internal long HighestStreamId
        {
            get
            {
                return _highestOpenedStreamId;
            }
            set
            {
                if (_highestOpenedStreamId < value)
                {
                    _highestOpenedStreamId = value;
                }
            }
        }

        private IKestrelTrace Log => _context.ServiceContext.Log;

        public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> httpApplication)
        {
            try
            {
                // Ensure TimeoutControl._lastTimestamp is initialized before anything that could set timeouts runs.
                _timeoutControl.Initialize(_systemClock.UtcNowTicks);

                var connectionHeartbeatFeature = _context.ConnectionFeatures.Get<IConnectionHeartbeatFeature>();
                var connectionLifetimeNotificationFeature = _context.ConnectionFeatures.Get<IConnectionLifetimeNotificationFeature>();

                // These features should never be null in Kestrel itself, if this middleware is ever refactored to run outside of kestrel,
                // we'll need to handle these missing.
                Debug.Assert(connectionHeartbeatFeature != null, nameof(IConnectionHeartbeatFeature) + " is missing!");
                Debug.Assert(connectionLifetimeNotificationFeature != null, nameof(IConnectionLifetimeNotificationFeature) + " is missing!");

                // Register the various callbacks once we're going to start processing requests

                // The heart beat for various timeouts
                connectionHeartbeatFeature?.OnHeartbeat(state => ((Http3Connection)state).Tick(), this);

                // Register for graceful shutdown of the server
                using var shutdownRegistration = connectionLifetimeNotificationFeature?.ConnectionClosedRequested.Register(state => ((Http3Connection)state).StopProcessingNextRequest(), this);

                // Register for connection close
                using var closedRegistration = _context.ConnectionContext.ConnectionClosed.Register(state => ((Http3Connection)state).OnConnectionClosed(), this);

                await InnerProcessRequestsAsync(httpApplication);
            }
            catch (Exception ex)
            {
                Log.LogCritical(0, ex, $"Unexpected exception in {nameof(Http3Connection)}.{nameof(ProcessRequestsAsync)}.");
            }
            finally
            {
            }
        }

        // For testing only
        internal void Initialize()
        {
        }

        public void StopProcessingNextRequest()
        {
            bool previousState;
            lock (_protocolSelectionLock)
            {
                previousState = _aborted;
            }

            // TODO figure out how to gracefully close next requests
        }

        public void OnConnectionClosed()
        {
            bool previousState;
            lock (_protocolSelectionLock)
            {
                previousState = _aborted;
            }

            // TODO figure out how to gracefully close next requests
        }

        public void Abort(ConnectionAbortedException ex)
        {
            bool previousState;

            lock (_protocolSelectionLock)
            {
                previousState = _aborted;
                _aborted = true;
            }

            if (!previousState)
            {
                InnerAbort(ex);
            }
        }

        public void Tick()
        {
            if (_aborted)
            {
                // It's safe to check for timeouts on a dead connection,
                // but try not to in order to avoid extraneous logs.
                return;
            }

            // It's safe to use UtcNowUnsynchronized since Tick is called by the Heartbeat.
            var now = _systemClock.UtcNowUnsynchronized;
            _timeoutControl.Tick(now);
        }

        public void OnTimeout(TimeoutReason reason)
        {
            // In the cases that don't log directly here, we expect the setter of the timeout to also be the input
            // reader, so when the read is canceled or aborted, the reader should write the appropriate log.
            switch (reason)
            {
                case TimeoutReason.KeepAlive:
                    StopProcessingNextRequest();
                    break;
                case TimeoutReason.RequestHeaders:
                    HandleRequestHeadersTimeout();
                    break;
                case TimeoutReason.ReadDataRate:
                    HandleReadDataRateTimeout();
                    break;
                case TimeoutReason.WriteDataRate:
                    Log.ResponseMinimumDataRateNotSatisfied(_context.ConnectionId, "" /*TraceIdentifier*/); // TODO trace identifier.
                    Abort(new ConnectionAbortedException(CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied));
                    break;
                case TimeoutReason.RequestBodyDrain:
                case TimeoutReason.TimeoutFeature:
                    Abort(new ConnectionAbortedException(CoreStrings.ConnectionTimedOutByServer));
                    break;
                default:
                    Debug.Assert(false, "Invalid TimeoutReason");
                    break;
            }
        }

        internal async Task InnerProcessRequestsAsync<TContext>(IHttpApplication<TContext> application)
        {
            // Start other three unidirectional streams here.
            var controlTask = CreateControlStream(application);
            var encoderTask = CreateEncoderStream(application);
            var decoderTask = CreateDecoderStream(application);

            try
            {
                while (true)
                {
                    var streamContext = await _multiplexedContext.AcceptAsync();
                    if (streamContext == null || _haveSentGoAway)
                    {
                        break;
                    }

                    var quicStreamFeature = streamContext.Features.Get<IStreamDirectionFeature>();
                    var streamIdFeature = streamContext.Features.Get<IStreamIdFeature>();

                    Debug.Assert(quicStreamFeature != null);

                    var httpConnectionContext = new Http3StreamContext
                    {
                        ConnectionId = streamContext.ConnectionId,
                        StreamContext = streamContext,
                        // TODO connection context is null here. Should we set it to anything?
                        ServiceContext = _context.ServiceContext,
                        ConnectionFeatures = streamContext.Features,
                        MemoryPool = _context.MemoryPool,
                        Transport = streamContext.Transport,
                        TimeoutControl = _context.TimeoutControl,
                        LocalEndPoint = streamContext.LocalEndPoint as IPEndPoint,
                        RemoteEndPoint = streamContext.RemoteEndPoint as IPEndPoint
                    };

                    if (!quicStreamFeature.CanWrite)
                    {
                        // Unidirectional stream
                        var stream = new Http3ControlStream<TContext>(application, this, httpConnectionContext);
                        ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
                    }
                    else
                    {
                        // Keep track of highest stream id seen for GOAWAY
                        var streamId = streamIdFeature.StreamId;
                        HighestStreamId = streamId;

                        var http3Stream = new Http3Stream<TContext>(application, this, httpConnectionContext);
                        var stream = http3Stream;
                        lock (_streams)
                        {
                            _streams[streamId] = http3Stream;
                        }
                        ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
                    }
                }
            }
            finally
            {
                // Abort all streams as connection has shutdown.
                lock (_streams)
                {
                    foreach (var stream in _streams.Values)
                    {
                        stream.Abort(new ConnectionAbortedException("Connection is shutting down."));
                    }
                }

                ControlStream?.Abort(new ConnectionAbortedException("Connection is shutting down."));
                EncoderStream?.Abort(new ConnectionAbortedException("Connection is shutting down."));
                DecoderStream?.Abort(new ConnectionAbortedException("Connection is shutting down."));

                await controlTask;
                await encoderTask;
                await decoderTask;
            }
        }

        private async ValueTask CreateControlStream<TContext>(IHttpApplication<TContext> application)
        {
            var stream = await CreateNewUnidirectionalStreamAsync(application);
            ControlStream = stream;
            await stream.SendStreamIdAsync(id: 0);
            await stream.SendSettingsFrameAsync();
        }

        private async ValueTask CreateEncoderStream<TContext>(IHttpApplication<TContext> application)
        {
            var stream = await CreateNewUnidirectionalStreamAsync(application);
            EncoderStream = stream;
            await stream.SendStreamIdAsync(id: 2);
        }

        private async ValueTask CreateDecoderStream<TContext>(IHttpApplication<TContext> application)
        {
            var stream = await CreateNewUnidirectionalStreamAsync(application);
            DecoderStream = stream;
            await stream.SendStreamIdAsync(id: 3);
        }

        private async ValueTask<Http3ControlStream> CreateNewUnidirectionalStreamAsync<TContext>(IHttpApplication<TContext> application)
        {
            var features = new FeatureCollection();
            features.Set<IStreamDirectionFeature>(new DefaultStreamDirectionFeature(canRead: false, canWrite: true));
            var streamContext = await _multiplexedContext.ConnectAsync(features);
            var httpConnectionContext = new Http3StreamContext
            {
                //ConnectionId = "", TODO getting stream ID from stream that isn't started throws an exception.
                StreamContext = streamContext,
                Protocols = HttpProtocols.Http3,
                ServiceContext = _context.ServiceContext,
                ConnectionFeatures = streamContext.Features,
                MemoryPool = _context.MemoryPool,
                Transport = streamContext.Transport,
                TimeoutControl = _context.TimeoutControl,
                LocalEndPoint = streamContext.LocalEndPoint as IPEndPoint,
                RemoteEndPoint = streamContext.RemoteEndPoint as IPEndPoint
            };

            return new Http3ControlStream<TContext>(application, this, httpConnectionContext);
        }

        public void HandleRequestHeadersTimeout()
        {
        }

        public void HandleReadDataRateTimeout()
        {
        }

        public void OnInputOrOutputCompleted()
        {
        }

        public void Tick(DateTimeOffset now)
        {
        }

        private void InnerAbort(ConnectionAbortedException ex)
        {
            lock (_sync)
            {
                if (ControlStream != null)
                {
                    // TODO need to await this somewhere or allow this to be called elsewhere?
                    ControlStream.SendGoAway(_highestOpenedStreamId).GetAwaiter().GetResult();
                }
            }

            _haveSentGoAway = true;

            // Abort currently active streams
            lock (_streams)
            {
                foreach (var stream in _streams.Values)
                {
                    stream.Abort(new ConnectionAbortedException("The Http3Connection has been aborted"), Http3ErrorCode.UnexpectedFrame);
                }
            }

            // TODO need to figure out if there is server initiated connection close rather than stream close?
        }

        public void ApplyMaxHeaderListSize(long value)
        {
            // TODO something here to call OnHeader?
        }

        internal void ApplyBlockedStream(long value)
        {
        }

        internal void ApplyMaxTableCapacity(long value)
        {
            // TODO make sure this works
            //_maxDynamicTableSize = value;
        }

        internal void RemoveStream(long streamId)
        {
            lock(_streams)
            {
                _streams.Remove(streamId);
            }
        }
    }
}
