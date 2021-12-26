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

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal class Http3Connection : IHttp3StreamLifetimeHandler, IRequestProcessor
{
    private static readonly object StreamPersistentStateKey = new object();

    // Internal for unit testing
    internal readonly Dictionary<long, IHttp3Stream> _streams = new Dictionary<long, IHttp3Stream>();
    internal IHttp3StreamLifetimeHandler _streamLifetimeHandler;

    // The highest opened request stream ID is sent with GOAWAY. The GOAWAY
    // value will signal to the peer to discard all requests with that value or greater.
    // When this value is sent, 4 will be added. We want 0 to be sent for no requests,
    // so start highest opened request stream ID at -4.
    private const long DefaultHighestOpenedRequestStreamId = -4;
    private long _highestOpenedRequestStreamId = DefaultHighestOpenedRequestStreamId;

    private readonly object _sync = new object();
    private readonly MultiplexedConnectionContext _multiplexedContext;
    private readonly HttpMultiplexedConnectionContext _context;
    private bool _aborted;
    private readonly object _protocolSelectionLock = new object();
    private int _gracefulCloseInitiator;
    private int _stoppedAcceptingStreams;
    private bool _gracefulCloseStarted;
    private int _activeRequestCount;
    private CancellationTokenSource _acceptStreamsCts = new CancellationTokenSource();
    private readonly Http3PeerSettings _serverSettings = new Http3PeerSettings();
    private readonly Http3PeerSettings _clientSettings = new Http3PeerSettings();
    private readonly StreamCloseAwaitable _streamCompletionAwaitable = new StreamCloseAwaitable();
    private readonly IProtocolErrorCodeFeature _errorCodeFeature;

    public Http3Connection(HttpMultiplexedConnectionContext context)
    {
        _multiplexedContext = (MultiplexedConnectionContext)context.ConnectionContext;
        _context = context;
        _streamLifetimeHandler = this;

        _errorCodeFeature = context.ConnectionFeatures.GetRequiredFeature<IProtocolErrorCodeFeature>();

        var httpLimits = context.ServiceContext.ServerOptions.Limits;

        _serverSettings.HeaderTableSize = (uint)httpLimits.Http3.HeaderTableSize;
        _serverSettings.MaxRequestHeaderFieldSectionSize = (uint)httpLimits.MaxRequestHeadersTotalSize;
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

            if (TryStopAcceptingStreams())
            {
                SendGoAwayAsync(GetCurrentGoAwayStreamId()).Preserve();
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

        UpdateStreamTimeouts(now);
    }

    private void UpdateStreamTimeouts(DateTimeOffset now)
    {
        // This method checks for timeouts:
        // 1. When a stream first starts and waits to receive headers.
        //    Uses RequestHeadersTimeout.
        // 2. When a stream finished and is waiting for underlying transport to drain.
        //    Uses MinResponseDataRate.

        var ticks = now.Ticks;

        lock (_streams)
        {
            foreach (var stream in _streams.Values)
            {
                if (stream.IsReceivingHeader)
                {
                    if (stream.StreamTimeoutTicks == default)
                    {
                        // On expiration overflow, use max value.
                        var expirationTicks = ticks + _context.ServiceContext.ServerOptions.Limits.RequestHeadersTimeout.Ticks;
                        stream.StreamTimeoutTicks = expirationTicks >= 0 ? expirationTicks : long.MaxValue;
                    }

                    if (stream.StreamTimeoutTicks < ticks)
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

                    if (stream.StreamTimeoutTicks == default)
                    {
                        stream.StreamTimeoutTicks = _context.TimeoutControl.GetResponseDrainDeadline(ticks, minDataRate);
                    }

                    if (stream.StreamTimeoutTicks < ticks)
                    {
                        // Cancel connection to be consistent with other data rate limits.
                        Log.ResponseMinimumDataRateNotSatisfied(_context.ConnectionId, stream.TraceIdentifier);
                        Abort(new ConnectionAbortedException(CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied), Http3ErrorCode.InternalError);
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

        try
        {
            outboundControlStream = await CreateNewUnidirectionalStreamAsync(application);
            lock (_sync)
            {
                OutboundControlStream = outboundControlStream;
            }

            // Don't delay on waiting to send outbound control stream settings.
            outboundControlStreamTask = ProcessOutboundControlStreamAsync(outboundControlStream);

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

                        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-5.2-2
                        if (_gracefulCloseStarted)
                        {
                            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.2-3
                            streamContext.Features.GetRequiredFeature<IProtocolErrorCodeFeature>().Error = (long)Http3ErrorCode.RequestRejected;
                            streamContext.Abort(new ConnectionAbortedException("HTTP/3 connection is closing and no longer accepts new requests."));
                            await streamContext.DisposeAsync();

                            continue;
                        }

                        // Request stream IDs are tracked.
                        UpdateHighestOpenedRequestStreamId(streamIdFeature.StreamId);

                        var persistentStateFeature = streamContext.Features.Get<IPersistentStateFeature>();
                        Debug.Assert(persistentStateFeature != null, $"Required {nameof(IPersistentStateFeature)} not on stream context.");

                        Http3Stream<TContext> stream;

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
            clientAbort = true;
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

                // Abort active request streams.
                lock (_streams)
                {
                    foreach (var stream in _streams.Values)
                    {
                        stream.Abort(CreateConnectionAbortError(error, clientAbort), (Http3ErrorCode)_errorCodeFeature.Error);
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

                // Complete
                Abort(CreateConnectionAbortError(error, clientAbort), (Http3ErrorCode)_errorCodeFeature.Error);

                // Wait for active requests to complete.
                while (_activeRequestCount > 0)
                {
                    await _streamCompletionAwaitable;
                }

                _context.TimeoutControl.CancelTimeout();
            }
            catch
            {
                Abort(CreateConnectionAbortError(error, clientAbort), Http3ErrorCode.InternalError);
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

    private Http3StreamContext CreateHttpStreamContext(ConnectionContext streamContext)
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
            _multiplexedContext.ConnectionId,
            HttpProtocols.Http3,
            _context.AltSvcHeader,
            _multiplexedContext,
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
        Debug.Assert(!stream.IsReceivingHeader);
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
        TryStopAcceptingStreams();

        // Abort the connection using the error code the client used. For a graceful close, this should be H3_NO_ERROR.
        Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient), (Http3ErrorCode)_errorCodeFeature.Error);
    }

    private static class GracefulCloseInitiator
    {
        public const int None = 0;
        public const int Server = 1;
        public const int Client = 2;
    }
}
