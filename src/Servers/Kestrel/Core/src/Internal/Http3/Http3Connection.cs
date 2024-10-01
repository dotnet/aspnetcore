// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;
using Microsoft.AspNetCore.Server.Kestrel.Core.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class Http3Connection : IHttp3StreamLifetimeHandler, IRequestProcessor
{
    internal static readonly object StreamPersistentStateKey = new();

    // Internal for unit testing
    internal IHttp3StreamLifetimeHandler _streamLifetimeHandler;
    internal readonly Dictionary<long, IHttp3Stream> _streams = new();
    internal readonly Dictionary<long, Http3PendingStream> _unidentifiedStreams = new();

    internal readonly MultiplexedConnectionContext _multiplexedContext;
    internal readonly Http3PeerSettings _serverSettings = new();
    internal readonly Http3PeerSettings _clientSettings = new();

    // The highest opened request stream ID is sent with GOAWAY. The GOAWAY
    // value will signal to the peer to discard all requests with that value or greater.
    // When this value is sent, 4 will be added. We want 0 to be sent for no requests,
    // so start highest opened request stream ID at -4.
    private const long DefaultHighestOpenedRequestStreamId = -4;

    private readonly Lock _sync = new();
    private readonly HttpMultiplexedConnectionContext _context;
    private readonly Lock _protocolSelectionLock = new();
    private readonly StreamCloseAwaitable _streamCompletionAwaitable = new();
    private readonly IProtocolErrorCodeFeature _errorCodeFeature;
    private readonly Dictionary<long, WebTransportSession>? _webtransportSessions;

    private long _highestOpenedRequestStreamId = DefaultHighestOpenedRequestStreamId;
    private bool _aborted;
    private int _gracefulCloseInitiator;
    private ConnectionEndReason _gracefulCloseReason;
    private int _stoppedAcceptingStreams;
    private bool _gracefulCloseStarted;
    private int _activeRequestCount;
    private CancellationTokenSource _acceptStreamsCts = new();

    public Http3Connection(HttpMultiplexedConnectionContext context)
    {
        _multiplexedContext = (MultiplexedConnectionContext)context.ConnectionContext;
        _context = context;
        _streamLifetimeHandler = this;
        MetricsContext = context.MetricsContext;
        _errorCodeFeature = context.ConnectionFeatures.GetRequiredFeature<IProtocolErrorCodeFeature>();

        var httpLimits = context.ServiceContext.ServerOptions.Limits;

        _serverSettings.HeaderTableSize = (uint)httpLimits.Http3.HeaderTableSize;
        _serverSettings.MaxRequestHeaderFieldSectionSize = (uint)httpLimits.MaxRequestHeadersTotalSize;
        _serverSettings.EnableWebTransport = Convert.ToUInt32(context.ServiceContext.ServerOptions.EnableWebTransportAndH3Datagrams);
        // technically these are 2 different settings so they should have separate values but the Chromium implementation requires
        // them to both be 1 to use WebTransport.
        _serverSettings.H3Datagram = Convert.ToUInt32(context.ServiceContext.ServerOptions.EnableWebTransportAndH3Datagrams);

        if (context.ServiceContext.ServerOptions.EnableWebTransportAndH3Datagrams)
        {
            _webtransportSessions = new();
        }
    }

    private void UpdateHighestOpenedRequestStreamId(long streamId)
    {
        // Only one thread will update the highest stream ID value at a time.
        // Additional thread safty not required.

        if (_highestOpenedRequestStreamId >= streamId)
        {
            // Double check here incase the streams are received out of order.
            return;
        }

        _highestOpenedRequestStreamId = streamId;
    }

    // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-5.2-2
    private long GetCurrentGoAwayStreamId() => Interlocked.Read(ref _highestOpenedRequestStreamId) + 4;

    private KestrelTrace Log => _context.ServiceContext.Log;
    public ConnectionMetricsContext MetricsContext { get; }
    public KestrelServerLimits Limits => _context.ServiceContext.ServerOptions.Limits;
    public Http3ControlStream? OutboundControlStream { get; set; }
    public Http3ControlStream? ControlStream { get; set; }
    public Http3ControlStream? EncoderStream { get; set; }
    public Http3ControlStream? DecoderStream { get; set; }
    public string ConnectionId => _context.ConnectionId;
    public ITimeoutControl TimeoutControl => _context.TimeoutControl;

    // The default error value is -1. If it hasn't been changed before abort is called then default to HTTP/3's NoError value.
    private Http3ErrorCode Http3ErrorCodeOrNoError => _errorCodeFeature.Error == -1 ? Http3ErrorCode.NoError : (Http3ErrorCode)_errorCodeFeature.Error;

    public void StopProcessingNextRequest(ConnectionEndReason reason)
        => StopProcessingNextRequest(serverInitiated: true, reason);

    public void StopProcessingNextRequest(bool serverInitiated, ConnectionEndReason reason)
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
                _gracefulCloseReason = reason;

                // Break out of AcceptStreams so connection state can be updated.
                _acceptStreamsCts.Cancel();
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
            TryStopAcceptingStreams();
            _multiplexedContext.Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient));
        }
    }

    private bool TryStopAcceptingStreams()
    {
        if (Interlocked.Exchange(ref _stoppedAcceptingStreams, 1) == 0)
        {
            return true;
        }

        return false;
    }

    public void Abort(ConnectionAbortedException ex, ConnectionEndReason reason)
    {
        Abort(ex, Http3ErrorCode.InternalError, reason);
    }

    public void Abort(ConnectionAbortedException ex, Http3ErrorCode errorCode, ConnectionEndReason reason)
    {
        bool previousState;

        lock (_protocolSelectionLock)
        {
            previousState = _aborted;
            _aborted = true;
        }

        if (_webtransportSessions is not null)
        {
            foreach (var session in _webtransportSessions)
            {
                if (ex.InnerException is not null)
                {
                    session.Value.Abort(new ConnectionAbortedException(ex.Message, ex.InnerException), errorCode);
                }
                else
                {
                    session.Value.Abort(new ConnectionAbortedException(ex.Message), errorCode);
                }
            }
        }

        if (!previousState)
        {
            _errorCodeFeature.Error = (long)errorCode;
            KestrelMetrics.AddConnectionEndReason(MetricsContext, reason);

            if (TryStopAcceptingStreams())
            {
                SendGoAwayAsync(GetCurrentGoAwayStreamId()).Preserve();
            }

            _multiplexedContext.Abort(ex);
        }
    }

    public void Tick(long timestamp)
    {
        if (_aborted)
        {
            // It's safe to check for timeouts on a dead connection,
            // but try not to in order to avoid extraneous logs.
            return;
        }

        ValidateOpenControlStreams(timestamp);
        UpdateStreamTimeouts(timestamp);
    }

    private void ValidateOpenControlStreams(long timestamp)
    {
        // This method validates that a connnection's control streams are open.
        //
        // They're checked on a delayed timer because when a connection is aborted or timed out, notifications are sent to open streams
        // and the connection simultaneously. This is a problem because when a control stream is closed the connection should be aborted
        // with the H3_CLOSED_CRITICAL_STREAM status. There is a race between the connection closing for the real reason, and control
        // streams closing the connection with H3_CLOSED_CRITICAL_STREAM.
        //
        // Realistically, control streams are never closed except when the connection is. A small delay in aborting the connection in the
        // unlikely situation where a control stream is incorrectly closed should be fine.
        ValidateOpenControlStream(OutboundControlStream, this, timestamp);
        ValidateOpenControlStream(ControlStream, this, timestamp);
        ValidateOpenControlStream(EncoderStream, this, timestamp);
        ValidateOpenControlStream(DecoderStream, this, timestamp);

        static void ValidateOpenControlStream(Http3ControlStream? stream, Http3Connection connection, long timestamp)
        {
            if (stream != null)
            {
                if (stream.IsCompleted || stream.IsAborted || stream.EndStreamReceived)
                {
                    // If a control stream is no longer active then set a timeout so that the connection is aborted next tick.
                    if (stream.StreamTimeoutTimestamp == default)
                    {
                        stream.StreamTimeoutTimestamp = timestamp;
                    }

                    if (stream.StreamTimeoutTimestamp < timestamp)
                    {
                        connection.OnStreamConnectionError(new Http3ConnectionErrorException(CoreStrings.Http3ErrorControlStreamClosed, Http3ErrorCode.ClosedCriticalStream, ConnectionEndReason.ClosedCriticalStream));
                    }
                }
            }
        }
    }

    private void UpdateStreamTimeouts(long timestamp)
    {
        // This method checks for timeouts:
        // 1. When a stream first starts and waits to receive headers.
        //    Uses RequestHeadersTimeout.
        // 2. When a stream finished and is waiting for underlying transport to drain.
        //    Uses MinResponseDataRate.
        var serviceContext = _context.ServiceContext;
        var requestHeadersTimeout = serviceContext.ServerOptions.Limits.RequestHeadersTimeout.ToTicks(
                        serviceContext.TimeProvider);

        lock (_unidentifiedStreams)
        {
            foreach (var stream in _unidentifiedStreams.Values)
            {
                if (stream.StreamTimeoutTimestamp == default)
                {
                    // On expiration overflow, use max value.
                    var expiration = timestamp + requestHeadersTimeout;
                    stream.StreamTimeoutTimestamp = expiration >= 0 ? expiration : long.MaxValue;
                }

                if (stream.StreamTimeoutTimestamp < timestamp)
                {
                    stream.Abort(new("Stream timed out before its type was determined."));
                }
            }
        }

        lock (_streams)
        {
            foreach (var stream in _streams.Values)
            {
                if (stream.IsReceivingHeader)
                {
                    if (stream.StreamTimeoutTimestamp == default)
                    {
                        // On expiration overflow, use max value.
                        var expiration = timestamp + requestHeadersTimeout;
                        stream.StreamTimeoutTimestamp = expiration >= 0 ? expiration : long.MaxValue;
                    }

                    if (stream.StreamTimeoutTimestamp < timestamp)
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
                else if (stream.IsDraining)
                {
                    var minDataRate = _context.ServiceContext.ServerOptions.Limits.MinResponseDataRate;
                    if (minDataRate == null)
                    {
                        continue;
                    }

                    if (stream.StreamTimeoutTimestamp == default)
                    {
                        stream.StreamTimeoutTimestamp = TimeoutControl.GetResponseDrainDeadline(timestamp, minDataRate);
                    }

                    if (stream.StreamTimeoutTimestamp < timestamp)
                    {
                        // Cancel connection to be consistent with other data rate limits.
                        Log.ResponseMinimumDataRateNotSatisfied(_context.ConnectionId, stream.TraceIdentifier);
                        OnStreamConnectionError(new Http3ConnectionErrorException(CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied, Http3ErrorCode.InternalError, ConnectionEndReason.MinResponseDataRate));
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
        Http3ControlStream? outboundControlStream = null;
        ValueTask outboundControlStreamTask = default;
        bool clientAbort = false;
        ConnectionEndReason reason = ConnectionEndReason.Unset;

        try
        {
            outboundControlStream = await CreateNewUnidirectionalStreamAsync(application);
            lock (_sync)
            {
                OutboundControlStream = outboundControlStream;
            }

            // Don't delay on waiting to send outbound control stream settings.
            outboundControlStreamTask = ProcessOutboundControlStreamAsync(outboundControlStream);

            // Close the connection if we don't receive any request streams
            TimeoutControl.SetTimeout(Limits.KeepAliveTimeout, TimeoutReason.KeepAlive);

            while (_stoppedAcceptingStreams == 0)
            {
                var streamContext = await _multiplexedContext.AcceptAsync(_acceptStreamsCts.Token);

                try
                {
                    // Return null on server close or cancellation.
                    if (streamContext == null)
                    {
                        if (_acceptStreamsCts.Token.IsCancellationRequested)
                        {
                            _acceptStreamsCts = new CancellationTokenSource();
                        }

                        // There is no stream so continue to skip to UpdateConnectionState in finally.
                        // UpdateConnectionState is responsible for updating connection to
                        // stop accepting streams and break out of accept loop.
                        continue;
                    }

                    var streamDirectionFeature = streamContext.Features.Get<IStreamDirectionFeature>();
                    var streamIdFeature = streamContext.Features.Get<IStreamIdFeature>();

                    Debug.Assert(streamDirectionFeature != null);
                    Debug.Assert(streamIdFeature != null);

                    // unidirectional stream
                    if (!streamDirectionFeature.CanWrite)
                    {
                        var context = CreateHttpStreamContext(streamContext);

                        if (_context.ServiceContext.ServerOptions.EnableWebTransportAndH3Datagrams)
                        {
                            var pendingStream = new Http3PendingStream(context, streamIdFeature.StreamId);

                            _streamLifetimeHandler.OnUnidentifiedStreamReceived(pendingStream);

                            // TODO: This needs to get dispatched off of the accept loop to avoid blocking other streams. (https://github.com/dotnet/aspnetcore/issues/42789)
                            var streamType = await pendingStream.ReadNextStreamHeaderAsync(context, streamIdFeature.StreamId, null);

                            _unidentifiedStreams.Remove(streamIdFeature.StreamId, out _);

                            if (streamType == (long)Http3StreamType.WebTransportUnidirectional)
                            {
                                await CreateAndAddWebTransportStream(pendingStream, streamIdFeature.StreamId, WebTransportStreamType.Input);
                            }
                            else
                            {
                                var controlStream = new Http3ControlStream<TContext>(application, context, streamType);
                                _streamLifetimeHandler.OnStreamCreated(controlStream);
                                ThreadPool.UnsafeQueueUserWorkItem(controlStream, preferLocal: false);
                            }
                        }
                        else
                        {
                            var controlStream = new Http3ControlStream<TContext>(application, context, null);
                            _streamLifetimeHandler.OnStreamCreated(controlStream);
                            ThreadPool.UnsafeQueueUserWorkItem(controlStream, preferLocal: false);
                        }
                    }
                    // bidirectional stream
                    else
                    {
                        if (_context.ServiceContext.ServerOptions.EnableWebTransportAndH3Datagrams)
                        {
                            var context = CreateHttpStreamContext(streamContext);
                            var pendingStream = new Http3PendingStream(context, streamIdFeature.StreamId);

                            _streamLifetimeHandler.OnUnidentifiedStreamReceived(pendingStream);

                            // TODO: This needs to get dispatched off of the accept loop to avoid blocking other streams. (https://github.com/dotnet/aspnetcore/issues/42789)
                            var streamType = await pendingStream.ReadNextStreamHeaderAsync(context, streamIdFeature.StreamId, Http3StreamType.WebTransportBidirectional);

                            _unidentifiedStreams.Remove(streamIdFeature.StreamId, out _);

                            if (streamType == (long)Http3StreamType.WebTransportBidirectional)
                            {
                                await CreateAndAddWebTransportStream(pendingStream, streamIdFeature.StreamId, WebTransportStreamType.Bidirectional);
                            }
                            else
                            {
                                await CreateHttp3Stream(streamContext, application, streamIdFeature.StreamId);
                            }
                        }
                        else
                        {
                            await CreateHttp3Stream(streamContext, application, streamIdFeature.StreamId);
                        }
                    }
                }
                catch (Http3PendingStreamException ex)
                {
                    _unidentifiedStreams.Remove(ex.StreamId, out var stream);
                    Log.Http3StreamAbort(CoreStrings.FormatUnidentifiedStream(ex.StreamId), Http3ErrorCode.StreamCreationError, new(ex.Message));
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
                    reason = ConnectionEndReason.ConnectionReset;
                }
            }
            error = ex;
            clientAbort = true;
        }
        catch (IOException ex)
        {
            Log.RequestProcessingError(_context.ConnectionId, ex);
            error = ex;
            reason = ConnectionEndReason.IOError;
        }
        catch (ConnectionAbortedException ex)
        {
            Log.RequestProcessingError(_context.ConnectionId, ex);
            error = ex;
            reason = ConnectionEndReason.OtherError;
        }
        catch (Http3ConnectionErrorException ex)
        {
            Log.Http3ConnectionError(_context.ConnectionId, ex);
            error = ex;
            reason = ex.Reason;
        }
        catch (Exception ex)
        {
            error = ex;
            reason = ConnectionEndReason.OtherError;
        }
        finally
        {
            try
            {
                // Don't try to send GOAWAY if the client has already closed the connection.
                if (!clientAbort)
                {
                    if (TryStopAcceptingStreams() || _gracefulCloseStarted)
                    {
                        await SendGoAwayAsync(GetCurrentGoAwayStreamId());
                    }
                }

                var errorCode = Http3ErrorCodeOrNoError;

                // Abort active request streams.
                lock (_streams)
                {
                    foreach (var stream in _streams.Values)
                    {
                        stream.Abort(CreateConnectionAbortError(error, clientAbort), errorCode);
                    }
                }

                lock (_unidentifiedStreams)
                {
                    foreach (var stream in _unidentifiedStreams.Values)
                    {
                        stream.Abort(CreateConnectionAbortError(error, clientAbort));
                    }
                }

                if (_webtransportSessions is not null)
                {
                    foreach (var session in _webtransportSessions.Values)
                    {
                        session.OnClientConnectionClosed();
                    }
                }

                if (outboundControlStream != null)
                {
                    // Don't gracefully close the outbound control stream. If the peer detects
                    // the control stream closes it will close with a procotol error.
                    // Instead, allow control stream to be automatically aborted when the
                    // connection is aborted.
                    await outboundControlStreamTask;
                }

                // Use graceful close reason if it has been set.
                if (reason == ConnectionEndReason.Unset && _gracefulCloseReason != ConnectionEndReason.Unset)
                {
                    reason = _gracefulCloseReason;
                }

                // Complete
                Abort(CreateConnectionAbortError(error, clientAbort), errorCode, reason);

                // Wait for active requests to complete.
                while (_activeRequestCount > 0)
                {
                    await _streamCompletionAwaitable;
                }

                TimeoutControl.CancelTimeout();
            }
            catch
            {
                Abort(CreateConnectionAbortError(error, clientAbort), Http3ErrorCode.InternalError, ConnectionEndReason.OtherError);
                throw;
            }
            finally
            {
                // Connection can close without processing any request streams.
                var streamId = _highestOpenedRequestStreamId != DefaultHighestOpenedRequestStreamId
                    ? _highestOpenedRequestStreamId
                    : (long?)null;

                Log.Http3ConnectionClosed(_context.ConnectionId, streamId);
            }
        }
    }

    private async Task CreateHttp3Stream<TContext>(ConnectionContext streamContext, IHttpApplication<TContext> application, long streamId) where TContext : notnull
    {
        // http request stream
        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-5.2-2
        if (_gracefulCloseStarted)
        {
            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.2-3
            streamContext.Features.GetRequiredFeature<IProtocolErrorCodeFeature>().Error = (long)Http3ErrorCode.RequestRejected;
            streamContext.Abort(new ConnectionAbortedException("HTTP/3 connection is closing and no longer accepts new requests."));
            await streamContext.DisposeAsync();

            return;
        }

        // Request stream IDs are tracked.
        UpdateHighestOpenedRequestStreamId(streamId);

        var persistentStateFeature = streamContext.Features.Get<IPersistentStateFeature>();
        Debug.Assert(persistentStateFeature != null, $"Required {nameof(IPersistentStateFeature)} not on stream context.");

        Http3Stream stream;

        // Check whether there is an existing HTTP/3 stream on the transport stream.
        // A stream will only be cached if the transport stream itself is reused.
        if (!persistentStateFeature.State.TryGetValue(StreamPersistentStateKey, out var s))
        {
            stream = new Http3Stream<TContext>(application, CreateHttpStreamContext(streamContext));
            persistentStateFeature.State.Add(StreamPersistentStateKey, stream);
        }
        else
        {
            stream = (Http3Stream<TContext>)s!;
            stream.InitializeWithExistingContext(streamContext.Transport);
        }

        _streamLifetimeHandler.OnStreamCreated(stream);
        KestrelEventSource.Log.RequestQueuedStart(stream, AspNetCore.Http.HttpProtocol.Http3);
        _context.ServiceContext.Metrics.RequestQueuedStart(MetricsContext, KestrelMetrics.Http3);

        ThreadPool.UnsafeQueueUserWorkItem(stream, preferLocal: false);
    }

    private async Task CreateAndAddWebTransportStream(Http3PendingStream stream, long streamId, WebTransportStreamType type)
    {
        Debug.Assert(_context.ServiceContext.ServerOptions.EnableWebTransportAndH3Datagrams);

        // TODO: This needs to get dispatched off of the accept loop to avoid blocking other streams. (https://github.com/dotnet/aspnetcore/issues/42789)
        var correspondingSession = await stream.ReadNextStreamHeaderAsync(stream.Context, streamId, null);

        lock (_webtransportSessions!)
        {
            if (!_webtransportSessions.TryGetValue(correspondingSession, out var session))
            {
                stream.Abort(new ConnectionAbortedException(CoreStrings.ReceivedLooseWebTransportStream));
                throw new Http3StreamErrorException(CoreStrings.ReceivedLooseWebTransportStream, Http3ErrorCode.StreamCreationError);
            }

            stream.Context.WebTransportSession = session;
            var webtransportStream = new WebTransportStream(stream.Context, type);
            session.AddStream(webtransportStream);
        }
    }

    private static ConnectionAbortedException CreateConnectionAbortError(Exception? error, bool clientAbort)
    {
        if (error is ConnectionAbortedException abortedException)
        {
            return abortedException;
        }

        if (clientAbort)
        {
            return new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient, error!);
        }

        return new ConnectionAbortedException(CoreStrings.Http3ConnectionFaulted, error!);
    }

    internal Http3StreamContext CreateHttpStreamContext(ConnectionContext streamContext)
    {
        var httpConnectionContext = new Http3StreamContext(
            _multiplexedContext.ConnectionId,
            HttpProtocols.Http3,
            _context.AltSvcHeader,
            _multiplexedContext,
            _context.ServiceContext,
            streamContext.Features,
            _context.MemoryPool,
            streamContext.LocalEndPoint as IPEndPoint,
            streamContext.RemoteEndPoint as IPEndPoint,
            streamContext,
            this)
        {
            TimeoutControl = _context.TimeoutControl,
            Transport = streamContext.Transport
        };

        return httpConnectionContext;
    }

    private void UpdateConnectionState()
    {
        if (_stoppedAcceptingStreams != 0)
        {
            return;
        }

        if (_gracefulCloseInitiator != GracefulCloseInitiator.None)
        {
            int activeRequestCount;
            lock (_streams)
            {
                activeRequestCount = _activeRequestCount;
            }

            if (!_gracefulCloseStarted)
            {
                _gracefulCloseStarted = true;

                _errorCodeFeature.Error = (long)Http3ErrorCode.NoError;
                Log.Http3ConnectionClosing(_context.ConnectionId);

                if (_gracefulCloseInitiator == GracefulCloseInitiator.Server && activeRequestCount > 0)
                {
                    // Go away with largest streamid to initiate graceful shutdown.
                    SendGoAwayAsync(VariableLengthIntegerHelper.EightByteLimit).Preserve();
                }
            }

            if (activeRequestCount == 0)
            {
                TryStopAcceptingStreams();
            }
        }
    }

    private async ValueTask ProcessOutboundControlStreamAsync(Http3ControlStream controlStream)
    {
        try
        {
            await controlStream.ProcessOutboundSendsAsync(id: 0);
        }
        catch (Exception ex)
        {
            Log.Http3OutboundControlStreamError(ConnectionId, ex);

            var connectionError = new Http3ConnectionErrorException(CoreStrings.Http3ControlStreamErrorInitializingOutbound, Http3ErrorCode.ClosedCriticalStream, ConnectionEndReason.ClosedCriticalStream);
            Log.Http3ConnectionError(ConnectionId, connectionError);

            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-6.2.1
            Abort(new ConnectionAbortedException(connectionError.Message, connectionError), connectionError.ErrorCode, ConnectionEndReason.ClosedCriticalStream);
        }
    }

    private async ValueTask<Http3ControlStream> CreateNewUnidirectionalStreamAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        var features = new FeatureCollection();
        features.Set<IStreamDirectionFeature>(new DefaultStreamDirectionFeature(canRead: false, canWrite: true));
        var streamContext = await _multiplexedContext.ConnectAsync(features);
        var httpConnectionContext = CreateHttpStreamContext(streamContext);

        return new Http3ControlStream<TContext>(application, httpConnectionContext, 0L);
    }

    private async ValueTask<FlushResult> SendGoAwayAsync(long id)
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

    void IHttp3StreamLifetimeHandler.OnUnidentifiedStreamReceived(Http3PendingStream stream)
    {
        lock (_unidentifiedStreams)
        {
            // place in a pending stream dictionary so we can track it (and timeout if necessary) as we don't have a proper stream instance yet
            _unidentifiedStreams.Add(stream.StreamId, stream);
        }
    }

    void IHttp3StreamLifetimeHandler.OnStreamCreated(IHttp3Stream stream)
    {
        lock (_streams)
        {
            if (stream.IsRequestStream)
            {
                if (_activeRequestCount == 0 && TimeoutControl.TimerReason == TimeoutReason.KeepAlive)
                {
                    TimeoutControl.CancelTimeout();
                }

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

                if (_activeRequestCount == 0)
                {
                    TimeoutControl.SetTimeout(Limits.KeepAliveTimeout, TimeoutReason.KeepAlive);
                }
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
        OnStreamConnectionError(ex);
    }

    private void OnStreamConnectionError(Http3ConnectionErrorException ex)
    {
        Log.Http3ConnectionError(ConnectionId, ex);
        Abort(new ConnectionAbortedException(ex.Message, ex), ex.ErrorCode, ex.Reason);
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
            case Http3SettingType.EnableWebTransport:
                _clientSettings.EnableWebTransport = (uint)value;
                break;
            case Http3SettingType.H3Datagram:
                _clientSettings.H3Datagram = (uint)value;
                break;
            default:
                throw new InvalidOperationException("Unexpected setting: " + type);
        }
    }

    void IHttp3StreamLifetimeHandler.OnStreamHeaderReceived(IHttp3Stream stream)
    {
        Debug.Assert(!stream.IsReceivingHeader);
    }

    public void HandleRequestHeadersTimeout()
    {
        Log.ConnectionBadRequest(ConnectionId, KestrelBadHttpRequestException.GetException(RequestRejectionReason.RequestHeadersTimeout));
        Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestHeadersTimeout), ConnectionEndReason.RequestHeadersTimeout);
    }

    public void HandleReadDataRateTimeout()
    {
        Debug.Assert(Limits.MinRequestBodyDataRate != null);

        Log.RequestBodyMinimumDataRateNotSatisfied(ConnectionId, null, Limits.MinRequestBodyDataRate.BytesPerSecond);
        Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestBodyTimeout), ConnectionEndReason.MinRequestBodyDataRate);
    }

    public void OnInputOrOutputCompleted()
    {
        TryStopAcceptingStreams();

        // Abort the connection using the error code the client used. For a graceful close, this should be H3_NO_ERROR.
        Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient), Http3ErrorCodeOrNoError, ConnectionEndReason.TransportCompleted);
    }

    internal WebTransportSession OpenNewWebTransportSession(Http3Stream http3Stream)
    {
        Debug.Assert(_context.ServiceContext.ServerOptions.EnableWebTransportAndH3Datagrams);

        WebTransportSession session;
        lock (_webtransportSessions!)
        {
            Debug.Assert(!_webtransportSessions.ContainsKey(http3Stream.StreamId));

            session = new WebTransportSession(this, http3Stream);
            _webtransportSessions[http3Stream.StreamId] = session;
        }
        return session;
    }

    private static class GracefulCloseInitiator
    {
        public const int None = 0;
        public const int Server = 1;
        public const int Client = 2;
    }
}
