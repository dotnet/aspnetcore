// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal class Http3Connection : IHttp3StreamLifetimeHandler, IRequestProcessor
    {
        private static readonly object StreamPersistentStateKey = new object();

        // Internal for unit testing
        internal readonly Dictionary<long, IHttp3Stream> _streams = new Dictionary<long, IHttp3Stream>();
        internal IHttp3StreamLifetimeHandler _streamLifetimeHandler;

        private long _highestOpenedStreamId;
        private readonly object _sync = new object();
        private readonly MultiplexedConnectionContext _multiplexedContext;
        private readonly HttpMultiplexedConnectionContext _context;
        private bool _aborted;
        private readonly object _protocolSelectionLock = new object();
        private int _gracefulCloseInitiator;
        private int _isClosed;
        private bool _gracefulCloseStarted;
        private int _activeRequestCount;

        private readonly Http3PeerSettings _serverSettings = new Http3PeerSettings();
        private readonly Http3PeerSettings _clientSettings = new Http3PeerSettings();
        private readonly StreamCloseAwaitable _streamCompletionAwaitable = new StreamCloseAwaitable();
        private readonly IProtocolErrorCodeFeature _errorCodeFeature;

        public Http3Connection(HttpMultiplexedConnectionContext context)
        {
            _multiplexedContext = (MultiplexedConnectionContext)context.ConnectionContext;
            _context = context;
            _streamLifetimeHandler = this;

            _errorCodeFeature = context.ConnectionFeatures.Get<IProtocolErrorCodeFeature>()!;

            var httpLimits = context.ServiceContext.ServerOptions.Limits;

            _serverSettings.HeaderTableSize = (uint)httpLimits.Http3.HeaderTableSize;
            _serverSettings.MaxRequestHeaderFieldSectionSize = (uint)httpLimits.MaxRequestHeadersTotalSize;
        }

        private void UpdateHighestStreamId(long streamId)
        {
            // Only one thread will update the highest stream ID value at a time.
            // Additional thread safty not required.

            if (_highestOpenedStreamId >= streamId)
            {
                // Double check here incase the streams are received out of order.
                return;
            }

            _highestOpenedStreamId = streamId;
        }

        private long GetHighestStreamId() => Interlocked.Read(ref _highestOpenedStreamId);

        private IKestrelTrace Log => _context.ServiceContext.Log;
        public KestrelServerLimits Limits => _context.ServiceContext.ServerOptions.Limits;
        public Http3ControlStream? OutboundControlStream { get; set; }
        public Http3ControlStream? ControlStream { get; set; }
        public Http3ControlStream? EncoderStream { get; set; }
        public Http3ControlStream? DecoderStream { get; set; }
        public string ConnectionId => _context.ConnectionId;

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
                    // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-5.2-11
                    // An endpoint that completes a graceful shutdown SHOULD use the H3_NO_ERROR error code
                    // when closing the connection.
                    _errorCodeFeature.Error = (long)Http3ErrorCode.NoError;

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
                Log.Http3ConnectionClosed(_context.ConnectionId, GetHighestStreamId());
                return true;
            }

            return false;
        }

        public void Abort(ConnectionAbortedException ex)
        {
            Abort(ex, Http3ErrorCode.InternalError);
        }

        public void Abort(ConnectionAbortedException ex, Http3ErrorCode errorCode)
        {
            bool previousState;

            lock (_protocolSelectionLock)
            {
                previousState = _aborted;
                _aborted = true;
            }

            if (!previousState)
            {
                _errorCodeFeature.Error = (long)errorCode;

                if (TryClose())
                {
                    SendGoAway(GetHighestStreamId()).Preserve();
                }

                _multiplexedContext.Abort(ex);
            }
        }

        public void Tick(DateTimeOffset now)
        {
            if (_aborted)
            {
                // It's safe to check for timeouts on a dead connection,
                // but try not to in order to avoid extraneous logs.
                return;
            }

            UpdateStartingStreams(now);
        }

        private void UpdateStartingStreams(DateTimeOffset now)
        {
            var ticks = now.Ticks;

            lock (_streams)
            {
                foreach (var stream in _streams.Values)
                {
                    if (stream.ReceivedHeader)
                    {
                        continue;
                    }

                    if (stream.HeaderTimeoutTicks == default)
                    {
                        // On expiration overflow, use max value.
                        var expirationTicks = ticks + _context.ServiceContext.ServerOptions.Limits.RequestHeadersTimeout.Ticks;
                        stream.HeaderTimeoutTicks = expirationTicks >= 0 ? expirationTicks : long.MaxValue;
                    }

                    if (stream.HeaderTimeoutTicks < ticks)
                    {
                        if (stream.IsRequestStream)
                        {
                            stream.Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestHeadersTimeout), Http3ErrorCode.RequestRejected);
                        }
                        else
                        {
                            stream.Abort(new ConnectionAbortedException(CoreStrings.Http3ControlStreamHeaderTimeout), Http3ErrorCode.StreamCreationError);
                        }
                    }
                }
            }
        }

        public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull
        {
            // An endpoint MAY avoid creating an encoder stream if it's not going to
            // be used(for example if its encoder doesn't wish to use the dynamic
            // table, or if the maximum size of the dynamic table permitted by the
            // peer is zero).

            // An endpoint MAY avoid creating a decoder stream if its decoder sets
            // the maximum capacity of the dynamic table to zero.

            // Don't create Encoder and Decoder as they aren't used now.

            Exception? error = null;
            ValueTask outboundControlStreamTask = default;

            try
            {
                var outboundControlStream = await CreateNewUnidirectionalStreamAsync(application);
                lock (_sync)
                {
                    OutboundControlStream = outboundControlStream;
                }

                // Don't delay on waiting to send outbound control stream settings.
                outboundControlStreamTask = ProcessOutboundControlStreamAsync(outboundControlStream);

                while (_isClosed == 0)
                {
                    // Don't pass a cancellation token to AcceptAsync.
                    // AcceptAsync will return null if the connection is gracefully shutting down or aborted.
                    var streamContext = await _multiplexedContext.AcceptAsync();
                    try
                    {
                        if (streamContext == null)
                        {
                            break;
                        }

                        var streamDirectionFeature = streamContext.Features.Get<IStreamDirectionFeature>();
                        var streamIdFeature = streamContext.Features.Get<IStreamIdFeature>();

                        Debug.Assert(streamDirectionFeature != null);
                        Debug.Assert(streamIdFeature != null);

                        if (!streamDirectionFeature.CanWrite)
                        {
                            // Unidirectional stream
                            var stream = new Http3ControlStream<TContext>(application, CreateHttpStreamContext(streamContext));
                            _streamLifetimeHandler.OnStreamCreated(stream);

                            ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
                        }
                        else
                        {
                            // Request stream
                            UpdateHighestStreamId(streamIdFeature.StreamId);

                            Http3Stream<TContext> stream;

                            // Check whether there is an existing HTTP/3 stream on the transport stream.
                            // A stream will only be cached if the transport stream itself is reused.
                            if (!streamContext.PersistentState.TryGetValue(StreamPersistentStateKey, out var s))
                            {
                                stream = new Http3Stream<TContext>(application, CreateHttpStreamContext(streamContext));
                                streamContext.PersistentState.Add(StreamPersistentStateKey, stream);
                            }
                            else
                            {
                                stream = (Http3Stream<TContext>)s!;
                                stream.InitializeWithExistingContext(streamContext.Transport);
                            }

                            _streamLifetimeHandler.OnStreamCreated(stream);

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
            catch (Http3ConnectionErrorException ex)
            {
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
                        await SendGoAway(GetHighestStreamId());
                    }

                    foreach (var stream in _streams.Values)
                    {
                        stream.Abort(connectionError, (Http3ErrorCode)_errorCodeFeature.Error);
                    }

                    lock (_sync)
                    {
                        OutboundControlStream?.Abort(connectionError, (Http3ErrorCode)_errorCodeFeature.Error);
                    }

                    while (_activeRequestCount > 0)
                    {
                        await _streamCompletionAwaitable;
                    }

                    await outboundControlStreamTask;

                    _context.TimeoutControl.CancelTimeout();
                    _context.TimeoutControl.StartDrainTimeout(Limits.MinResponseDataRate, Limits.MaxResponseBufferSize);
                }
                catch
                {
                    Abort(connectionError, Http3ErrorCode.InternalError);
                    throw;
                }
            }
        }

        private Http3StreamContext CreateHttpStreamContext(ConnectionContext streamContext)
        {
            var httpConnectionContext = new Http3StreamContext(
                streamContext.ConnectionId,
                protocols: default,
                connectionContext: null!, // TODO connection context is null here. Should we set it to anything?
                _context.ServiceContext,
                streamContext.Features,
                _context.MemoryPool,
                streamContext.LocalEndPoint as IPEndPoint,
                streamContext.RemoteEndPoint as IPEndPoint,
                _streamLifetimeHandler,
                streamContext,
                _clientSettings,
                _serverSettings);
            httpConnectionContext.TimeoutControl = _context.TimeoutControl;
            httpConnectionContext.Transport = streamContext.Transport;

            return httpConnectionContext;
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
                    SendGoAway(VariableLengthIntegerHelper.EightByteLimit).Preserve();
                }
            }

            if (activeRequestCount == 0)
            {
                if (_gracefulCloseStarted)
                {
                    if (TryClose())
                    {
                        SendGoAway(GetHighestStreamId()).Preserve();
                    }
                }
            }
        }

        private async ValueTask ProcessOutboundControlStreamAsync(Http3ControlStream controlStream)
        {
            try
            {
                await controlStream.SendStreamIdAsync(id: 0);
                await controlStream.SendSettingsFrameAsync();
            }
            catch (Exception ex)
            {
                Log.Http3OutboundControlStreamError(ConnectionId, ex);

                var connectionError = new Http3ConnectionErrorException(CoreStrings.Http3ControlStreamErrorInitializingOutbound, Http3ErrorCode.ClosedCriticalStream);
                Log.Http3ConnectionError(ConnectionId, connectionError);

                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-6.2.1
                Abort(new ConnectionAbortedException(connectionError.Message, connectionError), connectionError.ErrorCode);
            }
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
                _streamLifetimeHandler,
                streamContext,
                _clientSettings,
                _serverSettings);
            httpConnectionContext.TimeoutControl = _context.TimeoutControl;
            httpConnectionContext.Transport = streamContext.Transport;

            return new Http3ControlStream<TContext>(application, httpConnectionContext);
        }

        private async ValueTask<FlushResult> SendGoAway(long id)
        {
            Http3ControlStream? stream;
            lock (_sync)
            {
                stream = OutboundControlStream;
            }

            if (stream != null)
            {
                try
                {
                    return await stream.SendGoAway(id);
                }
                catch
                {
                    // The control stream may not be healthy.
                    // Ignore error sending go away.
                }
            }

            return default;
        }

        bool IHttp3StreamLifetimeHandler.OnInboundControlStream(Http3ControlStream stream)
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

        bool IHttp3StreamLifetimeHandler.OnInboundEncoderStream(Http3ControlStream stream)
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

        bool IHttp3StreamLifetimeHandler.OnInboundDecoderStream(Http3ControlStream stream)
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

        void IHttp3StreamLifetimeHandler.OnStreamCreated(IHttp3Stream stream)
        {
            lock (_streams)
            {
                if (stream.IsRequestStream)
                {
                    _activeRequestCount++;
                }
                _streams[stream.StreamId] = stream;
            }
        }

        void IHttp3StreamLifetimeHandler.OnStreamCompleted(IHttp3Stream stream)
        {
            lock (_streams)
            {
                if (stream.IsRequestStream)
                {
                    _activeRequestCount--;
                }
                _streams.Remove(stream.StreamId);
            }

            if (stream.IsRequestStream)
            {
                _streamCompletionAwaitable.Complete();
            }
        }

        void IHttp3StreamLifetimeHandler.OnStreamConnectionError(Http3ConnectionErrorException ex)
        {
            Log.Http3ConnectionError(ConnectionId, ex);
            Abort(new ConnectionAbortedException(ex.Message, ex), ex.ErrorCode);
        }

        void IHttp3StreamLifetimeHandler.OnInboundControlStreamSetting(Http3SettingType type, long value)
        {
            switch (type)
            {
                case Http3SettingType.QPackMaxTableCapacity:
                    break;
                case Http3SettingType.MaxFieldSectionSize:
                    _clientSettings.MaxRequestHeaderFieldSectionSize = (uint)value;
                    break;
                case Http3SettingType.QPackBlockedStreams:
                    break;
                default:
                    throw new InvalidOperationException("Unexpected setting: " + type);
            }
        }

        void IHttp3StreamLifetimeHandler.OnStreamHeaderReceived(IHttp3Stream stream)
        {
            Debug.Assert(stream.ReceivedHeader);
        }

        public void HandleRequestHeadersTimeout()
        {
            Log.ConnectionBadRequest(ConnectionId, KestrelBadHttpRequestException.GetException(RequestRejectionReason.RequestHeadersTimeout));
            Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestHeadersTimeout));
        }

        public void HandleReadDataRateTimeout()
        {
            Debug.Assert(Limits.MinRequestBodyDataRate != null);

            Log.RequestBodyMinimumDataRateNotSatisfied(ConnectionId, null, Limits.MinRequestBodyDataRate.BytesPerSecond);
            Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestBodyTimeout));
        }

        public void OnInputOrOutputCompleted()
        {
            TryClose();
            Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient), Http3ErrorCode.InternalError);
        }

        private static class GracefulCloseInitiator
        {
            public const int None = 0;
            public const int Server = 1;
            public const int Client = 2;
        }
    }
}
