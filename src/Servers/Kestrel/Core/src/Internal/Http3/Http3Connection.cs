// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Experimental;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3Connection : ITimeoutHandler
    {
        internal readonly Dictionary<long, Http3Stream> _streams = new Dictionary<long, Http3Stream>();

        private long _highestOpenedStreamId; // TODO lock to access
        private readonly object _sync = new object(); 
        private readonly MultiplexedConnectionContext _multiplexedContext;
        private readonly Http3ConnectionContext _context;
        private readonly ISystemClock _systemClock;
        private readonly TimeoutControl _timeoutControl;
        private bool _aborted;
        private readonly object _protocolSelectionLock = new object();
        private int _gracefulCloseInitiator;
        private int _isClosed;
        private bool _gracefulCloseStarted;
        private int _activeRequestCount = 0;

        private readonly Http3PeerSettings _serverSettings = new Http3PeerSettings();
        private readonly StreamCloseAwaitable _streamCompletionAwaitable = new StreamCloseAwaitable();


        public Http3Connection(Http3ConnectionContext context)
        {
            _multiplexedContext = context.ConnectionContext;
            _context = context;
            _systemClock = context.ServiceContext.SystemClock;
            _timeoutControl = new TimeoutControl(this);
            _context.TimeoutControl ??= _timeoutControl;

            var httpLimits = context.ServiceContext.ServerOptions.Limits;

            _serverSettings.HeaderTableSize = (uint)httpLimits.Http3.HeaderTableSize;
            _serverSettings.MaxRequestHeaderFieldSize = (uint)httpLimits.Http3.MaxRequestHeaderFieldSize;
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
        public KestrelServerLimits Limits => _context.ServiceContext.ServerOptions.Limits;
        public Http3ControlStream? OutboundControlStream { get; set; }
        public Http3ControlStream? ControlStream { get; set; }
        public Http3ControlStream? EncoderStream { get; set; }
        public Http3ControlStream? DecoderStream { get; set; }

        public async Task ProcessStreamsAsync<TContext>(IHttpApplication<TContext> httpApplication) where TContext : notnull
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
                using var shutdownRegistration = connectionLifetimeNotificationFeature?.ConnectionClosedRequested.Register(state => ((Http3Connection)state!).StopProcessingNextRequest(), this);

                // Register for connection close
                using var closedRegistration = _context.ConnectionContext.ConnectionClosed.Register(state => ((Http3Connection)state!).OnConnectionClosed(), this);

                await InnerProcessStreamsAsync(httpApplication);
            }
            catch (Exception ex)
            {
                Log.LogCritical(0, ex, $"Unexpected exception in {nameof(Http3Connection)}.{nameof(ProcessStreamsAsync)}.");
            }
        }

        public void StopProcessingNextRequest()
            => StopProcessingNextRequest(serverInitiated: true);

        public void StopProcessingNextRequest(bool serverInitiated)
        {
            bool previousState;
            lock (_protocolSelectionLock)
            {
                previousState = _aborted;
            }

            if (!previousState)
            {
                var initiator = serverInitiated ? GracefulCloseInitiator.Server : GracefulCloseInitiator.Client;

                if (Interlocked.CompareExchange(ref _gracefulCloseInitiator, initiator, GracefulCloseInitiator.None) == GracefulCloseInitiator.None)
                {
                    // Abort accept async loop to initiate graceful shutdown
                    // TODO aborting connection isn't graceful due to runtime issue, will drop data on streams
                    // Either we need to swap to using a cts here or fix runtime to gracefully close connection.
                    // await all stream being completed here.
                    _multiplexedContext.Abort();
                }
            }
        }

        public void OnConnectionClosed()
        {
            bool previousState;
            lock (_protocolSelectionLock)
            {
                previousState = _aborted;
            }

            if (!previousState)
            {
                TryClose();
                _multiplexedContext.Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient));
            }
        }

        private bool TryClose()
        {
            if (Interlocked.Exchange(ref _isClosed, 1) == 0)
            {
                Log.Http3ConnectionClosed(_context.ConnectionId, _highestOpenedStreamId);
                return true;
            }

            return false;
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
                if (TryClose())
                {
                    SendGoAway(_highestOpenedStreamId);
                }

                _multiplexedContext.Abort(ex);
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

            // TODO cancel process stream loop to update logic.
        }

        public void OnTimeout(TimeoutReason reason)
        {
            // In the cases that don't log directly here, we expect the setter of the timeout to also be the input
            // reader, so when the read is canceled or aborted, the reader should write the appropriate log.

            // TODO what timeouts should we handle here? Is keep alive something we should care about?
            switch (reason)
            {
                case TimeoutReason.KeepAlive:
                    SendGoAway(_highestOpenedStreamId);
                    break;
                case TimeoutReason.TimeoutFeature:
                    SendGoAway(_highestOpenedStreamId);
                    break;
                case TimeoutReason.RequestHeaders:
                case TimeoutReason.ReadDataRate:
                case TimeoutReason.WriteDataRate:
                case TimeoutReason.RequestBodyDrain:
                default:
                    Debug.Assert(false, "Invalid TimeoutReason");
                    break;
            }
        }

        internal async Task InnerProcessStreamsAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull
        {
            // An endpoint MAY avoid creating an encoder stream if it's not going to
            // be used(for example if its encoder doesn't wish to use the dynamic
            // table, or if the maximum size of the dynamic table permitted by the
            // peer is zero).

            // An endpoint MAY avoid creating a decoder stream if its decoder sets
            // the maximum capacity of the dynamic table to zero.

            // Don't create Encoder and Decoder as they aren't used now.
            Exception? error = null;

            var controlTask = CreateControlStream(application);

            _timeoutControl.SetTimeout(Limits.KeepAliveTimeout.Ticks, TimeoutReason.KeepAlive);

            try
            {
                while (_isClosed == 0)
                {
                    // TODO implement way to unblock loop for one call to accept async to update state.
                    // Use cts for now, update to custom awaitable or different solution in the future.
                    var streamContext = await _multiplexedContext.AcceptAsync();
                    try
                    {
                        if (streamContext == null)
                        {
                            break;
                        }

                        var quicStreamFeature = streamContext.Features.Get<IStreamDirectionFeature>();
                        var streamIdFeature = streamContext.Features.Get<IStreamIdFeature>();

                        Debug.Assert(quicStreamFeature != null);
                        Debug.Assert(streamIdFeature != null);

                        var httpConnectionContext = new Http3StreamContext(
                            streamContext.ConnectionId,
                            protocols: default,
                            connectionContext: null!, // TODO connection context is null here. Should we set it to anything?
                            _context.ServiceContext,
                            streamContext.Features,
                            _context.MemoryPool,
                            streamContext.LocalEndPoint as IPEndPoint,
                            streamContext.RemoteEndPoint as IPEndPoint,
                            streamContext.Transport,
                            streamContext,
                            _serverSettings);
                        httpConnectionContext.TimeoutControl = _context.TimeoutControl;

                        if (!quicStreamFeature.CanWrite)
                        {
                            // Unidirectional stream
                            var stream = new Http3ControlStream<TContext>(application, this, httpConnectionContext);
                            ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
                        }
                        else
                        {
                            var streamId = streamIdFeature.StreamId;

                            HighestStreamId = streamId;

                            var http3Stream = new Http3Stream<TContext>(application, this, httpConnectionContext);
                            var stream = http3Stream;
                            lock (_streams)
                            {
                                _activeRequestCount++;
                                _streams[streamId] = http3Stream;
                            }
                            KestrelEventSource.Log.RequestQueuedStart(stream, AspNetCore.Http.HttpProtocol.Http3);
                            ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
                        }
                    }
                    finally
                    {
                        UpdateConnectionState();
                    }
                }
            }
            catch (ConnectionResetException ex)
            {
                lock (_streams)
                {
                    if (_activeRequestCount > 0)
                    {
                        Log.RequestProcessingError(_context.ConnectionId, ex);
                    }
                }
                error = ex;
            }
            catch (IOException ex)
            {
                Log.RequestProcessingError(_context.ConnectionId, ex);
                error = ex;
            }
            catch (ConnectionAbortedException ex)
            {
                Log.RequestProcessingError(_context.ConnectionId, ex);
                error = ex;
            }
            catch (Http3ConnectionException ex)
            {
                // TODO Connection error code?
                Log.Http3ConnectionError(_context.ConnectionId, ex);
                error = ex;
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                var connectionError = error as ConnectionAbortedException
                    ?? new ConnectionAbortedException(CoreStrings.Http3ConnectionFaulted, error!);

                try
                {
                    if (TryClose())
                    {
                        // This throws when connection is shut down.
                        // TODO how to make it so we can distinguish between Abort from server vs client?
                        // Different exception catches?
                        await SendGoAway(_highestOpenedStreamId);
                    }

                    foreach (var stream in _streams.Values)
                    {
                        stream.Abort(connectionError);
                    }

                    while (_activeRequestCount > 0)
                    {
                        await _streamCompletionAwaitable;
                    }

                    _timeoutControl.CancelTimeout();
                }
                catch
                {
                    Abort(connectionError);
                    throw;
                }
            }
        }

        private void UpdateConnectionState()
        {
            if (_isClosed != 0)
            {
                return;
            }

            int activeRequestCount;
            lock (_streams)
            {
                activeRequestCount = _activeRequestCount;
            }

            if (_gracefulCloseInitiator != GracefulCloseInitiator.None && !_gracefulCloseStarted)
            {
                _gracefulCloseStarted = true;

                Log.Http3ConnectionClosing(_context.ConnectionId);

                // TODO add way to check active stream count?
                if (_gracefulCloseInitiator == GracefulCloseInitiator.Server && activeRequestCount > 0)
                {
                    // Go away with largest streamid to initiate graceful shutdown.
                    SendGoAway(VariableLengthIntegerHelper.EightByteLimit);
                }
            }

            if (activeRequestCount == 0)
            {
                if (_gracefulCloseStarted)
                {
                    if (TryClose())
                    {
                        SendGoAway(_highestOpenedStreamId);
                    }
                }
                else
                {
                    // TODO should keep-alive timeout be a thing for HTTP/3? MsQuic currently tracks this for us?
                    if (_timeoutControl.TimerReason == TimeoutReason.None)
                    {
                        _timeoutControl.SetTimeout(Limits.KeepAliveTimeout.Ticks, TimeoutReason.KeepAlive);
                    }

                    // Only reason should be keep-alive.
                    Debug.Assert(_timeoutControl.TimerReason == TimeoutReason.KeepAlive);
                }
            }
        }

        private async ValueTask CreateControlStream<TContext>(IHttpApplication<TContext> application) where TContext : notnull
        {
            var stream = await CreateNewUnidirectionalStreamAsync(application);
            lock (_sync)
            {
                OutboundControlStream = stream;
            }
            await stream.SendStreamIdAsync(id: 0);
            await stream.SendSettingsFrameAsync();
        }

        private async ValueTask<Http3ControlStream> CreateNewUnidirectionalStreamAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull
        {
            var features = new FeatureCollection();
            features.Set<IStreamDirectionFeature>(new DefaultStreamDirectionFeature(canRead: false, canWrite: true));
            var streamContext = await _multiplexedContext.ConnectAsync(features);
            var httpConnectionContext = new Http3StreamContext(
                connectionId: null!, // TODO getting stream ID from stream that isn't started throws an exception.
                HttpProtocols.Http3,
                connectionContext: null!, // TODO connection context is null here. Should we set it to anything?
                _context.ServiceContext,
                streamContext.Features,
                _context.MemoryPool,
                streamContext.LocalEndPoint as IPEndPoint,
                streamContext.RemoteEndPoint as IPEndPoint,
                streamContext.Transport,
                streamContext,
                _serverSettings);
            httpConnectionContext.TimeoutControl = _context.TimeoutControl;

            return new Http3ControlStream<TContext>(application, this, httpConnectionContext);
        }

        private ValueTask<FlushResult> SendGoAway(long id)
        {
            lock (_sync)
            {
                if (OutboundControlStream != null)
                {
                    return OutboundControlStream.SendGoAway(id);
                }
            }
            return new ValueTask<FlushResult>();
        }

        public void ApplyMaxHeaderListSize(long value)
        {
        }

        internal void ApplyBlockedStream(long value)
        {
        }

        internal void ApplyMaxTableCapacity(long value)
        {
        }

        internal void RemoveStream(long streamId)
        {
            lock (_streams)
            {
                _activeRequestCount--;
                _streams.Remove(streamId);
            }

            _streamCompletionAwaitable.Complete();
        }

        public bool SetInboundControlStream(Http3ControlStream stream)
        {
            lock (_sync)
            {
                if (ControlStream == null)
                {
                    ControlStream = stream;
                    return true;
                }
                return false;
            }
        }

        public bool SetInboundEncoderStream(Http3ControlStream stream)
        {
            lock (_sync)
            {
                if (EncoderStream == null)
                {
                    EncoderStream = stream;
                    return true;
                }
                return false;
            }
        }

        public bool SetInboundDecoderStream(Http3ControlStream stream)
        {
            lock (_sync)
            {
                if (DecoderStream == null)
                {
                    DecoderStream = stream;
                    return true;
                }
                return false;
            }
        }

        private static class GracefulCloseInitiator
        {
            public const int None = 0;
            public const int Server = 1;
            public const int Client = 2;
        }
    }
}
