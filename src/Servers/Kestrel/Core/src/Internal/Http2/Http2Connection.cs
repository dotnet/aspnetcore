// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.HPack;
using System.Security.Authentication;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal sealed partial class Http2Connection : IHttp2StreamLifetimeHandler, IHttpStreamHeadersHandler, IRequestProcessor
{
    // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    private static ReadOnlySpan<byte> ClientPrefaceBytes => "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"u8;
    private static ReadOnlySpan<byte> AuthorityBytes => ":authority"u8;
    private static ReadOnlySpan<byte> MethodBytes => ":method"u8;
    private static ReadOnlySpan<byte> PathBytes => ":path"u8;
    private static ReadOnlySpan<byte> SchemeBytes => ":scheme"u8;
    private static ReadOnlySpan<byte> StatusBytes => ":status"u8;
    private static ReadOnlySpan<byte> ConnectionBytes => "connection"u8;
    private static ReadOnlySpan<byte> TeBytes => "te"u8;
    private static ReadOnlySpan<byte> TrailersBytes => "trailers"u8;
    private static ReadOnlySpan<byte> ConnectBytes => "CONNECT"u8;
    private static ReadOnlySpan<byte> ProtocolBytes => ":protocol"u8;

    public static ReadOnlySpan<byte> ClientPreface => ClientPrefaceBytes;
    public static byte[]? InvalidHttp1xErrorResponseBytes;

    private const PseudoHeaderFields _mandatoryRequestPseudoHeaderFields =
        PseudoHeaderFields.Method | PseudoHeaderFields.Path | PseudoHeaderFields.Scheme;

    private const int InitialStreamPoolSize = 5;
    private const int MaxStreamPoolSize = 100;
    private readonly TimeSpan StreamPoolExpiry = TimeSpan.FromSeconds(5);

    /// Increase this value to be more lenient (disconnect fewer clients).
    /// A non-positive value will disable the limit.
    /// The default value is 20, which was determined empirically using a toy attack client.
    /// The count is measured across a (non-configurable) 5 tick (i.e. second) window:
    /// if the count ever exceeds 5 * the limit, the connection is aborted.
    /// Note that this means that the limit can kick in before 5 ticks have elapsed.
    /// See <see cref="EnhanceYourCalmTickWindowCount"/>.
    /// TODO (https://github.com/dotnet/aspnetcore/issues/51308): make this configurable.
    private const string MaximumEnhanceYourCalmCountProperty = "Microsoft.AspNetCore.Server.Kestrel.Http2.MaxEnhanceYourCalmCount";

    // Internal for testing.
    internal static readonly int EnhanceYourCalmMaximumCount = GetMaximumEnhanceYourCalmCount();

    private static int GetMaximumEnhanceYourCalmCount()
    {
        var data = AppContext.GetData(MaximumEnhanceYourCalmCountProperty);

        // Programmatically-configured values are usually ints
        if (data is int count)
        {
            return count;
        }

        // msbuild-configured values are usually strings
        if (data is string countStr && int.TryParse(countStr, out var parsed))
        {
            return parsed;
        }

        return 20; // Empirically derived
    }

    // Accumulate _enhanceYourCalmCount over the course of EnhanceYourCalmTickWindowCount ticks.
    // This should make bursts less likely to trigger disconnects.
    // Internal for testing.
    internal const int EnhanceYourCalmTickWindowCount = 5;

    private static bool IsEnhanceYourCalmLimitEnabled => EnhanceYourCalmMaximumCount > 0;

    private readonly HttpConnectionContext _context;
    private readonly ConnectionMetricsContext _metricsContext;
    private readonly IConnectionMetricsTagsFeature? _metricsTagsFeature;
    private readonly Http2FrameWriter _frameWriter;
    private readonly Pipe _input;
    private readonly Task _inputTask;
    private readonly int _minAllocBufferSize;
    private readonly HPackDecoder _hpackDecoder;
    private readonly InputFlowControl _inputFlowControl;

    private readonly Http2PeerSettings _serverSettings = new Http2PeerSettings();
    private readonly Http2PeerSettings _clientSettings = new Http2PeerSettings();

    private readonly Http2Frame _incomingFrame = new Http2Frame();

    // This is only set to true by tests.
    private readonly bool _scheduleInline;

    private Http2Stream? _currentHeadersStream;
    private RequestHeaderParsingState _requestHeaderParsingState;
    private PseudoHeaderFields _parsedPseudoHeaderFields;
    private Http2HeadersFrameFlags _headerFlags;
    private int _totalParsedHeaderSize;
    private bool _isMethodConnect;
    private int _highestOpenedStreamId;
    private bool _gracefulCloseStarted;

    private int _clientActiveStreamCount;
    private int _serverActiveStreamCount;

    // Test hook to force sending EYC on *every* stream creation
    internal bool SendEnhanceYourCalmOnStartStream { set; private get; }
    private int _enhanceYourCalmCount;
    private int _tickCount;

    // The following are the only fields that can be modified outside of the ProcessRequestsAsync loop.
    private readonly ConcurrentQueue<Http2Stream> _completedStreams = new ConcurrentQueue<Http2Stream>();
    private readonly StreamCloseAwaitable _streamCompletionAwaitable = new StreamCloseAwaitable();
    private int _gracefulCloseInitiator;
    private ConnectionEndReason _gracefulCloseReason;
    private int _isClosed;

    // Internal for testing
    internal readonly Http2KeepAlive? _keepAlive;
    internal readonly Dictionary<int, Http2Stream> _streams = new Dictionary<int, Http2Stream>();
    internal PooledStreamStack<Http2Stream> StreamPool;
    internal IHttp2StreamLifetimeHandler _streamLifetimeHandler;

    public Http2Connection(HttpConnectionContext context)
    {
        var httpLimits = context.ServiceContext.ServerOptions.Limits;
        var http2Limits = httpLimits.Http2;

        _context = context;
        _streamLifetimeHandler = this;
        _metricsContext = context.MetricsContext;
        _metricsTagsFeature = context.ConnectionFeatures.Get<IConnectionMetricsTagsFeature>();

        // Capture the ExecutionContext before dispatching HTTP/2 middleware. Will be restored by streams when processing request
        _context.InitialExecutionContext = ExecutionContext.Capture();

        _input = new Pipe(GetInputPipeOptions());

        _minAllocBufferSize = context.MemoryPool.GetMinimumAllocSize();

        _hpackDecoder = new HPackDecoder(http2Limits.HeaderTableSize, http2Limits.MaxRequestHeaderFieldSize);

        var connectionWindow = (uint)http2Limits.InitialConnectionWindowSize;
        _inputFlowControl = new InputFlowControl(connectionWindow, connectionWindow / 2);

        if (http2Limits.KeepAlivePingDelay != TimeSpan.MaxValue)
        {
            _keepAlive = new Http2KeepAlive(
                http2Limits.KeepAlivePingDelay,
                http2Limits.KeepAlivePingTimeout,
                context.ServiceContext.TimeProvider);
        }

        _serverSettings.MaxConcurrentStreams = (uint)http2Limits.MaxStreamsPerConnection;
        _serverSettings.MaxFrameSize = (uint)http2Limits.MaxFrameSize;
        _serverSettings.HeaderTableSize = (uint)http2Limits.HeaderTableSize;
        _serverSettings.MaxHeaderListSize = (uint)httpLimits.MaxRequestHeadersTotalSize;
        _serverSettings.InitialWindowSize = (uint)http2Limits.InitialStreamWindowSize;

        // Start pool off at a smaller size if the max number of streams is less than the InitialStreamPoolSize
        StreamPool = new PooledStreamStack<Http2Stream>(Math.Min(InitialStreamPoolSize, http2Limits.MaxStreamsPerConnection));

        _scheduleInline = context.ServiceContext.Scheduler == PipeScheduler.Inline;

        _inputTask = CopyPipeAsync(_context.Transport.Input, _input.Writer);

        _frameWriter = new Http2FrameWriter(
            context.Transport.Output,
            context.ConnectionContext,
            this,
            (int)Math.Min(MaxTrackedStreams, int.MaxValue),
            context.TimeoutControl,
            httpLimits.MinResponseDataRate,
            context.ConnectionId,
            context.MemoryPool,
            context.ServiceContext);
    }

    public string ConnectionId => _context.ConnectionId;

    public PipeReader Input => _input.Reader;

    public KestrelTrace Log => _context.ServiceContext.Log;
    public IFeatureCollection ConnectionFeatures => _context.ConnectionFeatures;
    public TimeProvider TimeProvider => _context.ServiceContext.TimeProvider;
    public ITimeoutControl TimeoutControl => _context.TimeoutControl;
    public KestrelServerLimits Limits => _context.ServiceContext.ServerOptions.Limits;

    internal Http2PeerSettings ServerSettings => _serverSettings;

    // Max tracked streams is double max concurrent streams.
    // If a small MaxConcurrentStreams value is configured then still track at least to 100 streams
    // to support clients that send a burst of streams while the connection is being established.
    internal uint MaxTrackedStreams => Math.Max(_serverSettings.MaxConcurrentStreams * 2, 100);

    public void OnInputOrOutputCompleted()
    {
        var hasActiveStreams = _clientActiveStreamCount != 0;
        if (TryClose())
        {
            SetConnectionErrorCode(hasActiveStreams ? ConnectionEndReason.ConnectionReset : ConnectionEndReason.TransportCompleted, Http2ErrorCode.NO_ERROR);
        }
        var useException = _context.ServiceContext.ServerOptions.FinOnError || hasActiveStreams;
        _frameWriter.Abort(useException ? new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient) : null!);
    }

    private void SetConnectionErrorCode(ConnectionEndReason reason, Http2ErrorCode errorCode)
    {
        Debug.Assert(_isClosed == 1, "Should only be set when connection is closed.");

        KestrelMetrics.AddConnectionEndReason(_metricsContext, reason);
    }

    public void Abort(ConnectionAbortedException ex, ConnectionEndReason reason)
    {
        Abort(ex, Http2ErrorCode.INTERNAL_ERROR, reason);
    }

    public void Abort(ConnectionAbortedException ex, Http2ErrorCode errorCode, ConnectionEndReason reason)
    {
        if (TryClose())
        {
            SetConnectionErrorCode(reason, errorCode);
            _frameWriter.WriteGoAwayAsync(int.MaxValue, errorCode).Preserve();
        }

        _frameWriter.Abort(ex);
    }

    public void StopProcessingNextRequest(ConnectionEndReason reason)
        => StopProcessingNextRequest(serverInitiated: true, reason);

    public void HandleRequestHeadersTimeout()
    {
        Log.ConnectionBadRequest(ConnectionId, KestrelBadHttpRequestException.GetException(RequestRejectionReason.RequestHeadersTimeout));
        Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestHeadersTimeout), Http2ErrorCode.INTERNAL_ERROR, ConnectionEndReason.RequestHeadersTimeout);
    }

    public void HandleReadDataRateTimeout()
    {
        Debug.Assert(Limits.MinRequestBodyDataRate != null);

        Log.RequestBodyMinimumDataRateNotSatisfied(ConnectionId, null, Limits.MinRequestBodyDataRate.BytesPerSecond);
        Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestBodyTimeout), Http2ErrorCode.INTERNAL_ERROR, ConnectionEndReason.MinRequestBodyDataRate);
    }

    public void StopProcessingNextRequest(bool serverInitiated, ConnectionEndReason reason)
    {
        var initiator = serverInitiated ? GracefulCloseInitiator.Server : GracefulCloseInitiator.Client;

        if (Interlocked.CompareExchange(ref _gracefulCloseInitiator, initiator, GracefulCloseInitiator.None) == GracefulCloseInitiator.None)
        {
            _gracefulCloseReason = reason;
            Input.CancelPendingRead();
        }
    }

    public async Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        Exception? error = null;
        var errorCode = Http2ErrorCode.NO_ERROR;
        var reason = ConnectionEndReason.Unset;

        try
        {
            ValidateTlsRequirements();

            TimeoutControl.InitializeHttp2(_inputFlowControl);
            TimeoutControl.SetTimeout(Limits.KeepAliveTimeout, TimeoutReason.KeepAlive);

            if (!await TryReadPrefaceAsync())
            {
                reason = ConnectionEndReason.TransportCompleted;
                return;
            }

            if (_isClosed == 0)
            {
                await _frameWriter.WriteSettingsAsync(_serverSettings.GetNonProtocolDefaults());
                // Inform the client that the connection window is larger than the default. It can't be lowered here,
                // It can only be lowered by not issuing window updates after data is received.
                var connectionWindow = _context.ServiceContext.ServerOptions.Limits.Http2.InitialConnectionWindowSize;
                var diff = connectionWindow - (int)Http2PeerSettings.DefaultInitialWindowSize;
                if (diff > 0)
                {
                    await _frameWriter.WriteWindowUpdateAsync(0, diff);
                }
            }

            while (_isClosed == 0)
            {
                var result = await Input.ReadAsync();
                var buffer = result.Buffer;

                // Call UpdateCompletedStreams() prior to frame processing in order to remove any streams that have exceeded their drain timeouts.
                UpdateCompletedStreams();

                if (result.IsCanceled)
                {
                    // Heartbeat will cancel ReadAsync and trigger expiring unused streams from pool.
                    StreamPool.RemoveExpired(TimeProvider.GetTimestamp());
                }

                try
                {
                    bool frameReceived = false;
                    while (Http2FrameReader.TryReadFrame(ref buffer, _incomingFrame, _serverSettings.MaxFrameSize, out var framePayload))
                    {
                        frameReceived = true;
                        Log.Http2FrameReceived(ConnectionId, _incomingFrame);

                        try
                        {
                            await ProcessFrameAsync(application, framePayload);
                        }
                        catch (Http2StreamErrorException ex)
                        {
                            Log.Http2StreamError(ConnectionId, ex);
                            // The client doesn't know this error is coming, allow draining additional frames for now.
                            AbortStream(_incomingFrame.StreamId, new IOException(ex.Message, ex));

                            await _frameWriter.WriteRstStreamAsync(ex.StreamId, ex.ErrorCode);

                            // Resume reading frames after aborting this HTTP/2 stream.
                            // This is important because additional frames could be
                            // in the current buffer. We don't want to delay reading
                            // them until the next incoming read/heartbeat.
                        }
                    }

                    if (result.IsCompleted)
                    {
                        reason = ConnectionEndReason.TransportCompleted;
                        return;
                    }

                    if (_keepAlive != null)
                    {
                        // Note that the keep alive uses a complete frame being received to reset state.
                        // Some other keep alive implementations use any bytes being received to reset state.
                        var state = _keepAlive.ProcessKeepAlive(frameReceived);
                        if (state == KeepAliveState.SendPing)
                        {
                            await _frameWriter.WritePingAsync(Http2PingFrameFlags.NONE, Http2KeepAlive.PingPayload);
                        }
                        else if (state == KeepAliveState.Timeout)
                        {
                            // There isn't a good error code to return with the GOAWAY.
                            // NO_ERROR isn't a good choice because it indicates the connection is gracefully shutting down.
                            throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorKeepAliveTimeout, Http2ErrorCode.INTERNAL_ERROR, ConnectionEndReason.KeepAliveTimeout);
                        }
                    }
                }
                finally
                {
                    Input.AdvanceTo(buffer.Start, buffer.End);

                    UpdateConnectionState();
                }
            }
        }
        catch (ConnectionResetException ex)
        {
            // Don't log ECONNRESET errors when there are no active streams on the connection. Browsers like IE will reset connections regularly.
            if (_clientActiveStreamCount > 0)
            {
                Log.RequestProcessingError(ConnectionId, ex);
                reason = ConnectionEndReason.ConnectionReset;
            }
            else
            {
                reason = ConnectionEndReason.TransportCompleted;
            }

            error = ex;
        }
        catch (IOException ex)
        {
            Log.RequestProcessingError(ConnectionId, ex);
            error = ex;
            reason = ConnectionEndReason.IOError;
        }
        catch (ConnectionAbortedException ex)
        {
            Log.RequestProcessingError(ConnectionId, ex);
            error = ex;
        }
        catch (Http2ConnectionErrorException ex)
        {
            Log.Http2ConnectionError(ConnectionId, ex);
            error = ex;
            errorCode = ex.ErrorCode;
            reason = ex.Reason;
        }
        catch (HPackDecodingException ex)
        {
            Debug.Assert(_currentHeadersStream != null);

            Log.HPackDecodingError(ConnectionId, _currentHeadersStream.StreamId, ex);
            error = ex;
            errorCode = Http2ErrorCode.COMPRESSION_ERROR;
            reason = ConnectionEndReason.ErrorReadingHeaders;
        }
        catch (Exception ex)
        {
            Log.LogWarning(0, ex, CoreStrings.RequestProcessingEndError);
            error = ex;
            errorCode = Http2ErrorCode.INTERNAL_ERROR;
            reason = ConnectionEndReason.OtherError;
        }
        finally
        {
            var connectionError = error as ConnectionAbortedException
                ?? new ConnectionAbortedException(CoreStrings.Http2ConnectionFaulted, error!);

            try
            {
                if (TryClose())
                {
                    SetConnectionErrorCode(reason, errorCode);
                    await _frameWriter.WriteGoAwayAsync(_highestOpenedStreamId, errorCode);
                }

                // Ensure aborting each stream doesn't result in unnecessary WINDOW_UPDATE frames being sent.
                _inputFlowControl.StopWindowUpdates();

                foreach (var stream in _streams.Values)
                {
                    stream.Abort(new IOException(CoreStrings.Http2StreamAborted, connectionError));
                }

                // TODO (https://github.com/dotnet/aspnetcore/issues/51307):
                // For some reason, this loop doesn't terminate when we're trying to abort.
                if (!IsEnhanceYourCalmLimitEnabled || error is not Http2ConnectionErrorException)
                {
                    // Use the server _serverActiveStreamCount to drain all requests on the server side.
                    // Can't use _clientActiveStreamCount now as we now decrement that count earlier/
                    // Can't use _streams.Count as we wait for RST/END_STREAM before removing the stream from the dictionary
                    while (_serverActiveStreamCount > 0)
                    {
                        await _streamCompletionAwaitable;
                        UpdateCompletedStreams();
                    }
                }

                while (StreamPool.TryPop(out var pooledStream))
                {
                    pooledStream.Dispose();
                }

                // This cancels keep-alive and request header timeouts, but not the response drain timeout.
                TimeoutControl.CancelTimeout();
                TimeoutControl.StartDrainTimeout(Limits.MinResponseDataRate, Limits.MaxResponseBufferSize);

                _frameWriter.Complete();
            }
            catch
            {
                _frameWriter.Abort(connectionError);
                throw;
            }
            finally
            {
                Input.Complete();
                _context.Transport.Input.CancelPendingRead();
                await _inputTask;
                await _frameWriter.ShutdownAsync();
            }
        }
    }

    // https://tools.ietf.org/html/rfc7540#section-9.2
    // Some of these could not be checked in advance. Fail before using the connection.
    private void ValidateTlsRequirements()
    {
        var tlsFeature = ConnectionFeatures.Get<ITlsHandshakeFeature>();
        if (tlsFeature == null)
        {
            // Not using TLS at all.
            return;
        }

        if (tlsFeature.Protocol < SslProtocols.Tls12)
        {
            throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorMinTlsVersion(tlsFeature.Protocol), Http2ErrorCode.INADEQUATE_SECURITY, ConnectionEndReason.InsufficientTlsVersion);
        }
    }

    [Flags]
    private enum ReadPrefaceState
    {
        None = 0,
        Preface = 1,
        Http1x = 2,
        All = Preface | Http1x
    }

    private async Task<bool> TryReadPrefaceAsync()
    {
        // HTTP/1.x and HTTP/2 support connections without TLS. That means ALPN hasn't been used to ensure both sides are
        // using the same protocol. A common problem is someone using HTTP/1.x to talk to a HTTP/2 only endpoint.
        //
        // HTTP/2 starts a connection with a preface. This method reads and validates it. If the connection doesn't start
        // with the preface, and it isn't using TLS, then we attempt to detect what the client is trying to do and send
        // back a friendly error message.
        //
        // Outcomes from this method:
        // 1. Successfully read HTTP/2 preface. Connection continues to be established.
        // 2. Detect HTTP/1.x request. Send back HTTP/1.x 400 response.
        // 3. Unknown content. Report HTTP/2 PROTOCOL_ERROR to client.
        // 4. Timeout while waiting for content.
        //
        // Future improvement: Detect TLS frame. Useful for people starting TLS connection with a non-TLS endpoint.
        var state = ReadPrefaceState.All;

        // With TLS, ALPN should have already errored if the wrong HTTP version is used.
        // Only perform additional validation if endpoint doesn't use TLS.
        if (ConnectionFeatures.Get<ITlsHandshakeFeature>() != null)
        {
            state ^= ReadPrefaceState.Http1x;
        }

        while (_isClosed == 0)
        {
            var result = await Input.ReadAsync();
            var readableBuffer = result.Buffer;
            var consumed = readableBuffer.Start;
            var examined = readableBuffer.End;

            try
            {
                if (!readableBuffer.IsEmpty)
                {
                    if (state.HasFlag(ReadPrefaceState.Preface))
                    {
                        if (readableBuffer.Length >= ClientPreface.Length)
                        {
                            if (IsPreface(readableBuffer, out consumed, out examined))
                            {
                                return true;
                            }
                            else
                            {
                                state ^= ReadPrefaceState.Preface;
                            }
                        }
                    }

                    if (state.HasFlag(ReadPrefaceState.Http1x))
                    {
                        if (ParseHttp1x(readableBuffer, out var detectedVersion))
                        {
                            if (detectedVersion == HttpVersion.Http10 || detectedVersion == HttpVersion.Http11)
                            {
                                Log.PossibleInvalidHttpVersionDetected(ConnectionId, HttpVersion.Http2, detectedVersion);

                                var responseBytes = InvalidHttp1xErrorResponseBytes ??= Encoding.ASCII.GetBytes(
                                    "HTTP/1.1 400 Bad Request\r\n" +
                                    "Connection: close\r\n" +
                                    "Content-Type: text/plain\r\n" +
                                    "Content-Length: 56\r\n" +
                                    "\r\n" +
                                    "An HTTP/1.x request was sent to an HTTP/2 only endpoint.");

                                await _context.Transport.Output.WriteAsync(responseBytes);

                                // Close connection here so a GOAWAY frame isn't written.
                                if (TryClose())
                                {
                                    SetConnectionErrorCode(ConnectionEndReason.InvalidHttpVersion, Http2ErrorCode.PROTOCOL_ERROR);
                                }

                                return false;
                            }
                            else
                            {
                                state ^= ReadPrefaceState.Http1x;
                            }
                        }
                    }

                    // Tested all states. Return HTTP/2 protocol error.
                    if (state == ReadPrefaceState.None)
                    {
                        throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorInvalidPreface, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidHandshake);
                    }
                }

                if (result.IsCompleted)
                {
                    return false;
                }
            }
            finally
            {
                Input.AdvanceTo(consumed, examined);

                UpdateConnectionState();
            }
        }

        return false;
    }

    private bool ParseHttp1x(ReadOnlySequence<byte> buffer, out HttpVersion httpVersion)
    {
        httpVersion = HttpVersion.Unknown;

        var reader = new SequenceReader<byte>(buffer.Length > Limits.MaxRequestLineSize ? buffer.Slice(0, Limits.MaxRequestLineSize) : buffer);
        if (reader.TryReadTo(out ReadOnlySpan<byte> requestLine, (byte)'\n'))
        {
            // Line should be long enough for HTTP/1.X and end with \r\n
            if (requestLine.Length > 10 && requestLine[requestLine.Length - 1] == (byte)'\r')
            {
                httpVersion = HttpUtilities.GetKnownVersion(requestLine.Slice(requestLine.Length - 9, 8));
            }

            return true;
        }

        // Couldn't find newline within max request line size so this isn't valid HTTP/1.x.
        if (buffer.Length > Limits.MaxRequestLineSize)
        {
            return true;
        }

        return false;
    }

    private static bool IsPreface(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
    {
        consumed = buffer.Start;
        examined = buffer.End;

        Debug.Assert(buffer.Length >= ClientPreface.Length, "Not enough content to match preface.");

        var preface = buffer.Slice(0, ClientPreface.Length);
        var span = preface.ToSpan();

        if (!span.SequenceEqual(ClientPreface))
        {
            return false;
        }

        consumed = examined = preface.End;
        return true;
    }

    private Task ProcessFrameAsync<TContext>(IHttpApplication<TContext> application, in ReadOnlySequence<byte> payload) where TContext : notnull
    {
        // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1.1
        // Streams initiated by a client MUST use odd-numbered stream identifiers; ...
        // An endpoint that receives an unexpected stream identifier MUST respond with
        // a connection error (Section 5.4.1) of type PROTOCOL_ERROR.
        if (_incomingFrame.StreamId != 0 && (_incomingFrame.StreamId & 1) == 0)
        {
            throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdEven(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidStreamId);
        }

        return _incomingFrame.Type switch
        {
            Http2FrameType.DATA => ProcessDataFrameAsync(payload),
            Http2FrameType.HEADERS => ProcessHeadersFrameAsync(application, payload),
            Http2FrameType.PRIORITY => ProcessPriorityFrameAsync(),
            Http2FrameType.RST_STREAM => ProcessRstStreamFrameAsync(),
            Http2FrameType.SETTINGS => ProcessSettingsFrameAsync(payload),
            Http2FrameType.PUSH_PROMISE => throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorPushPromiseReceived, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.UnexpectedFrame),
            Http2FrameType.PING => ProcessPingFrameAsync(payload),
            Http2FrameType.GOAWAY => ProcessGoAwayFrameAsync(),
            Http2FrameType.WINDOW_UPDATE => ProcessWindowUpdateFrameAsync(),
            Http2FrameType.CONTINUATION => ProcessContinuationFrameAsync(payload),
            _ => ProcessUnknownFrameAsync(),
        };
    }

    private Task ProcessDataFrameAsync(in ReadOnlySequence<byte> payload)
    {
        if (_currentHeadersStream != null)
        {
            throw CreateHeadersInterleavedException();
        }

        if (_incomingFrame.StreamId == 0)
        {
            throw CreateStreamIdZeroException();
        }

        if (_incomingFrame.DataHasPadding && _incomingFrame.DataPadLength >= _incomingFrame.PayloadLength)
        {
            throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorPaddingTooLong(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidDataPadding);
        }

        ThrowIfIncomingFrameSentToIdleStream();

        if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
        {
            if (stream.RstStreamReceived)
            {
                // Hard abort, do not allow any more frames on this stream.
                throw CreateReceivedFrameStreamAbortedException(stream);
            }

            if (stream.EndStreamReceived)
            {
                // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1
                //
                // ...an endpoint that receives any frames after receiving a frame with the
                // END_STREAM flag set MUST treat that as a connection error (Section 5.4.1)
                // of type STREAM_CLOSED, unless the frame is permitted as described below.
                //
                // (The allowed frame types for this situation are WINDOW_UPDATE, RST_STREAM and PRIORITY)
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(_incomingFrame.Type, stream.StreamId), Http2ErrorCode.STREAM_CLOSED, ConnectionEndReason.FrameAfterStreamClose);
            }

            return stream.OnDataAsync(_incomingFrame, payload);
        }

        // If we couldn't find the stream, it was either alive previously but closed with
        // END_STREAM or RST_STREAM, or it was implicitly closed when the client opened
        // a new stream with a higher ID. Per the spec, we should send RST_STREAM if
        // the stream was closed with RST_STREAM or implicitly, but the spec also says
        // in http://httpwg.org/specs/rfc7540.html#rfc.section.5.4.1 that
        //
        // An endpoint can end a connection at any time. In particular, an endpoint MAY
        // choose to treat a stream error as a connection error.
        //
        // We choose to do that here so we don't have to keep state to track implicitly closed
        // streams vs. streams closed with END_STREAM or RST_STREAM.
        throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamClosed(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.STREAM_CLOSED, ConnectionEndReason.UnknownStream);
    }

    private Http2ConnectionErrorException CreateReceivedFrameStreamAbortedException(Http2Stream stream)
    {
        return new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamAborted(_incomingFrame.Type, stream.StreamId), Http2ErrorCode.STREAM_CLOSED, ConnectionEndReason.FrameAfterStreamClose);
    }

    private Http2ConnectionErrorException CreateStreamIdZeroException()
    {
        return new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidStreamId);
    }

    private Http2ConnectionErrorException CreateStreamIdNotZeroException()
    {
        return new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdNotZero(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidStreamId);
    }

    private Http2ConnectionErrorException CreateHeadersInterleavedException()
    {
        Debug.Assert(_currentHeadersStream != null, "Only throw this error if parsing headers.");
        return new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorHeadersInterleaved(_incomingFrame.Type, _incomingFrame.StreamId, _currentHeadersStream.StreamId), Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.UnexpectedFrame);
    }

    private Http2ConnectionErrorException CreateUnexpectedFrameLengthException(int expectedLength)
    {
        return new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorUnexpectedFrameLength(_incomingFrame.Type, expectedLength), Http2ErrorCode.FRAME_SIZE_ERROR, ConnectionEndReason.InvalidFrameLength);
    }

    private Task ProcessHeadersFrameAsync<TContext>(IHttpApplication<TContext> application, in ReadOnlySequence<byte> payload) where TContext : notnull
    {
        if (_currentHeadersStream != null)
        {
            throw CreateHeadersInterleavedException();
        }

        if (_incomingFrame.StreamId == 0)
        {
            throw CreateStreamIdZeroException();
        }

        if (_incomingFrame.HeadersHasPadding && _incomingFrame.HeadersPadLength >= _incomingFrame.PayloadLength - 1)
        {
            throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorPaddingTooLong(_incomingFrame.Type), Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidDataPadding);
        }

        if (_incomingFrame.HeadersHasPriority && _incomingFrame.HeadersStreamDependency == _incomingFrame.StreamId)
        {
            throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamSelfDependency(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.StreamSelfDependency);
        }

        if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
        {
            if (stream.RstStreamReceived)
            {
                // Hard abort, do not allow any more frames on this stream.
                throw CreateReceivedFrameStreamAbortedException(stream);
            }

            // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1
            //
            // ...an endpoint that receives any frames after receiving a frame with the
            // END_STREAM flag set MUST treat that as a connection error (Section 5.4.1)
            // of type STREAM_CLOSED, unless the frame is permitted as described below.
            //
            // (The allowed frame types after END_STREAM are WINDOW_UPDATE, RST_STREAM and PRIORITY)
            if (stream.EndStreamReceived)
            {
                throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamHalfClosedRemote(_incomingFrame.Type, stream.StreamId), Http2ErrorCode.STREAM_CLOSED, ConnectionEndReason.FrameAfterStreamClose);
            }

            // This is the last chance for the client to send END_STREAM
            if (!_incomingFrame.HeadersEndStream)
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorHeadersWithTrailersNoEndStream, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.MissingStreamEnd);
            }

            // Since we found an active stream, this HEADERS frame contains trailers
            _currentHeadersStream = stream;
            _requestHeaderParsingState = RequestHeaderParsingState.Trailers;

            var headersPayload = payload.Slice(0, _incomingFrame.HeadersPayloadLength); // Minus padding
            return DecodeTrailersAsync(_incomingFrame.HeadersEndHeaders, headersPayload);
        }
        else if (_incomingFrame.StreamId <= _highestOpenedStreamId)
        {
            // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1.1
            //
            // The first use of a new stream identifier implicitly closes all streams in the "idle"
            // state that might have been initiated by that peer with a lower-valued stream identifier.
            //
            // If we couldn't find the stream, it was previously closed (either implicitly or with
            // END_STREAM or RST_STREAM).
            throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamClosed(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.STREAM_CLOSED, ConnectionEndReason.InvalidStreamId);
        }
        else
        {
            // Cancel keep-alive timeout and start header timeout if necessary.
            if (TimeoutControl.TimerReason != TimeoutReason.None)
            {
                Debug.Assert(TimeoutControl.TimerReason == TimeoutReason.KeepAlive, "Non keep-alive timeout set at start of stream.");
                TimeoutControl.CancelTimeout();
            }

            if (!_incomingFrame.HeadersEndHeaders)
            {
                TimeoutControl.SetTimeout(Limits.RequestHeadersTimeout, TimeoutReason.RequestHeaders);
            }

            // Start a new stream
            _currentHeadersStream = GetStream(application);

            _headerFlags = _incomingFrame.HeadersFlags;

            var headersPayload = payload.Slice(0, _incomingFrame.HeadersPayloadLength); // Minus padding
            return DecodeHeadersAsync(_incomingFrame.HeadersEndHeaders, headersPayload);
        }
    }

    private Http2Stream GetStream<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        if (StreamPool.TryPop(out var stream))
        {
            stream.InitializeWithExistingContext(_incomingFrame.StreamId);
            return stream;
        }

        return new Http2Stream<TContext>(
            application,
            CreateHttp2StreamContext());
    }

    private Http2StreamContext CreateHttp2StreamContext()
    {
        var streamContext = new Http2StreamContext(
            ConnectionId,
            protocols: default,
            _context.AltSvcHeader,
            _context.ConnectionContext,
            _context.ServiceContext,
            _context.ConnectionFeatures,
            _context.MemoryPool,
            _context.LocalEndPoint,
            _context.RemoteEndPoint,
            _incomingFrame.StreamId,
            _streamLifetimeHandler,
            _clientSettings,
            _serverSettings,
            _frameWriter,
            _inputFlowControl,
            _metricsContext);
        streamContext.TimeoutControl = _context.TimeoutControl;
        streamContext.InitialExecutionContext = _context.InitialExecutionContext;

        return streamContext;
    }

    private Task ProcessPriorityFrameAsync()
    {
        if (_currentHeadersStream != null)
        {
            throw CreateHeadersInterleavedException();
        }

        if (_incomingFrame.StreamId == 0)
        {
            throw CreateStreamIdZeroException();
        }

        if (_incomingFrame.PriorityStreamDependency == _incomingFrame.StreamId)
        {
            throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamSelfDependency(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.StreamSelfDependency);
        }

        if (_incomingFrame.PayloadLength != 5)
        {
            throw CreateUnexpectedFrameLengthException(expectedLength: 5);
        }

        return Task.CompletedTask;
    }

    private Task ProcessRstStreamFrameAsync()
    {
        if (_currentHeadersStream != null)
        {
            throw CreateHeadersInterleavedException();
        }

        if (_incomingFrame.StreamId == 0)
        {
            throw CreateStreamIdZeroException();
        }

        if (_incomingFrame.PayloadLength != 4)
        {
            throw CreateUnexpectedFrameLengthException(expectedLength: 4);
        }

        ThrowIfIncomingFrameSentToIdleStream();

        if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
        {
            // Second reset
            if (stream.RstStreamReceived)
            {
                // https://tools.ietf.org/html/rfc7540#section-5.1
                // If RST_STREAM has already been received then the stream is in a closed state.
                // Additional frames (other than PRIORITY) are a stream error.
                // The server will usually send a RST_STREAM for a stream error, but RST_STREAM
                // shouldn't be sent in response to RST_STREAM to avoid a loop.
                // The best course of action here is to do nothing.
                return Task.CompletedTask;
            }

            // No additional inbound header or data frames are allowed for this stream after receiving a reset.
            stream.AbortRstStreamReceived();
        }

        return Task.CompletedTask;
    }

    private Task ProcessSettingsFrameAsync(in ReadOnlySequence<byte> payload)
    {
        if (_currentHeadersStream != null)
        {
            throw CreateHeadersInterleavedException();
        }

        if (_incomingFrame.StreamId != 0)
        {
            throw CreateStreamIdNotZeroException();
        }

        if (_incomingFrame.SettingsAck)
        {
            if (_incomingFrame.PayloadLength != 0)
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorSettingsAckLengthNotZero, Http2ErrorCode.FRAME_SIZE_ERROR, ConnectionEndReason.InvalidFrameLength);
            }

            return Task.CompletedTask;
        }

        if (_incomingFrame.PayloadLength % 6 != 0)
        {
            throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorSettingsLengthNotMultipleOfSix, Http2ErrorCode.FRAME_SIZE_ERROR, ConnectionEndReason.InvalidFrameLength);
        }

        try
        {
            // int.MaxValue is the largest allowed windows size.
            var previousInitialWindowSize = (int)_clientSettings.InitialWindowSize;
            var previousMaxFrameSize = _clientSettings.MaxFrameSize;

            _clientSettings.Update(Http2FrameReader.ReadSettings(payload));

            // Ack before we update the windows, they could send data immediately.
            var ackTask = _frameWriter.WriteSettingsAckAsync();

            if (_clientSettings.MaxFrameSize != previousMaxFrameSize)
            {
                // Don't let the client choose an arbitrarily large size, this will be used for response buffers.
                // Safe cast, MaxFrameSize is limited to 2^24-1 bytes by the protocol and by Http2PeerSettings.
                // Ref: https://datatracker.ietf.org/doc/html/rfc7540#section-4.2
                _frameWriter.UpdateMaxFrameSize((int)Math.Min(_clientSettings.MaxFrameSize, _serverSettings.MaxFrameSize));
            }

            // This difference can be negative.
            var windowSizeDifference = (int)_clientSettings.InitialWindowSize - previousInitialWindowSize;

            if (windowSizeDifference != 0)
            {
                foreach (var stream in _streams.Values)
                {
                    if (!stream.TryUpdateOutputWindow(windowSizeDifference))
                    {
                        // This means that this caused a stream window to become larger than int.MaxValue.
                        // This can never happen with a well behaved client and MUST be treated as a connection error.
                        // https://httpwg.org/specs/rfc7540.html#rfc.section.6.9.2
                        throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorInitialWindowSizeInvalid, Http2ErrorCode.FLOW_CONTROL_ERROR, ConnectionEndReason.InvalidSettings);
                    }
                }
            }

            // Maximum HPack encoder size is limited by Http2Limits.HeaderTableSize, configured max the server.
            //
            // Note that the client HPack decoder doesn't care about the ACK so we don't need to lock sending the
            // ACK and updating the table size on the server together.
            // The client will wait until a size agreed upon by it (sent in SETTINGS_HEADER_TABLE_SIZE) and the
            // server (sent as a dynamic table size update in the next HEADERS frame) is received before applying
            // the new size.
            _frameWriter.UpdateMaxHeaderTableSize(Math.Min(_clientSettings.HeaderTableSize, (uint)Limits.Http2.HeaderTableSize));

            return ackTask.GetAsTask();
        }
        catch (Http2SettingsParameterOutOfRangeException ex)
        {
            var errorCode = ex.Parameter == Http2SettingsParameter.SETTINGS_INITIAL_WINDOW_SIZE
                ? Http2ErrorCode.FLOW_CONTROL_ERROR
                : Http2ErrorCode.PROTOCOL_ERROR;

            throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorSettingsParameterOutOfRange(ex.Parameter), errorCode, ConnectionEndReason.InvalidSettings);
        }
    }

    private Task ProcessPingFrameAsync(in ReadOnlySequence<byte> payload)
    {
        if (_currentHeadersStream != null)
        {
            throw CreateHeadersInterleavedException();
        }

        if (_incomingFrame.StreamId != 0)
        {
            throw CreateStreamIdNotZeroException();
        }

        if (_incomingFrame.PayloadLength != 8)
        {
            throw CreateUnexpectedFrameLengthException(expectedLength: 8);
        }

        // Incoming ping resets connection keep alive timeout
        if (TimeoutControl.TimerReason == TimeoutReason.KeepAlive)
        {
            TimeoutControl.ResetTimeout(Limits.KeepAliveTimeout, TimeoutReason.KeepAlive);
        }

        if (_incomingFrame.PingAck)
        {
            // TODO: verify that payload is equal to the outgoing PING frame
            return Task.CompletedTask;
        }

        return _frameWriter.WritePingAsync(Http2PingFrameFlags.ACK, payload).GetAsTask();
    }

    private Task ProcessGoAwayFrameAsync()
    {
        if (_currentHeadersStream != null)
        {
            throw CreateHeadersInterleavedException();
        }

        if (_incomingFrame.StreamId != 0)
        {
            throw CreateStreamIdNotZeroException();
        }

        // StopProcessingNextRequest must be called before RequestClose to ensure it's considered client initiated.
        StopProcessingNextRequest(serverInitiated: false, ConnectionEndReason.ClientGoAway);
        _context.ConnectionFeatures.Get<IConnectionLifetimeNotificationFeature>()?.RequestClose();

        return Task.CompletedTask;
    }

    private Task ProcessWindowUpdateFrameAsync()
    {
        if (_currentHeadersStream != null)
        {
            throw CreateHeadersInterleavedException();
        }

        if (_incomingFrame.PayloadLength != 4)
        {
            throw CreateUnexpectedFrameLengthException(expectedLength: 4);
        }

        ThrowIfIncomingFrameSentToIdleStream();

        if (_incomingFrame.WindowUpdateSizeIncrement == 0)
        {
            // http://httpwg.org/specs/rfc7540.html#rfc.section.6.9
            // A receiver MUST treat the receipt of a WINDOW_UPDATE
            // frame with an flow-control window increment of 0 as a
            // stream error (Section 5.4.2) of type PROTOCOL_ERROR;
            // errors on the connection flow-control window MUST be
            // treated as a connection error (Section 5.4.1).
            //
            // http://httpwg.org/specs/rfc7540.html#rfc.section.5.4.1
            // An endpoint can end a connection at any time. In
            // particular, an endpoint MAY choose to treat a stream
            // error as a connection error.
            //
            // Since server initiated stream resets are not yet properly
            // implemented and tested, we treat all zero length window
            // increments as connection errors for now.
            throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorWindowUpdateIncrementZero, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidWindowUpdateSize);
        }

        if (_incomingFrame.StreamId == 0)
        {
            if (!_frameWriter.TryUpdateConnectionWindow(_incomingFrame.WindowUpdateSizeIncrement))
            {
                throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorWindowUpdateSizeInvalid, Http2ErrorCode.FLOW_CONTROL_ERROR, ConnectionEndReason.InvalidWindowUpdateSize);
            }
        }
        else if (_streams.TryGetValue(_incomingFrame.StreamId, out var stream))
        {
            if (stream.RstStreamReceived)
            {
                // Hard abort, do not allow any more frames on this stream.
                throw CreateReceivedFrameStreamAbortedException(stream);
            }

            if (!stream.TryUpdateOutputWindow(_incomingFrame.WindowUpdateSizeIncrement))
            {
                throw new Http2StreamErrorException(_incomingFrame.StreamId, CoreStrings.Http2ErrorWindowUpdateSizeInvalid, Http2ErrorCode.FLOW_CONTROL_ERROR);
            }
        }
        else
        {
            // The stream was not found in the dictionary which means the stream was probably closed. This can
            // happen when the client sends a window update for a stream right as the server closes the same stream
            // Since this is an unavoidable race, we just ignore the window update frame.
        }

        return Task.CompletedTask;
    }

    private Task ProcessContinuationFrameAsync(in ReadOnlySequence<byte> payload)
    {
        if (_currentHeadersStream == null)
        {
            throw new Http2ConnectionErrorException(CoreStrings.Http2ErrorContinuationWithNoHeaders, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.UnexpectedFrame);
        }

        if (_incomingFrame.StreamId != _currentHeadersStream.StreamId)
        {
            throw CreateHeadersInterleavedException();
        }

        if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
        {
            return DecodeTrailersAsync(_incomingFrame.ContinuationEndHeaders, payload);
        }
        else
        {
            Debug.Assert(TimeoutControl.TimerReason == TimeoutReason.RequestHeaders, "Received continuation frame without request header timeout being set.");

            if (_incomingFrame.HeadersEndHeaders)
            {
                TimeoutControl.CancelTimeout();
            }

            return DecodeHeadersAsync(_incomingFrame.ContinuationEndHeaders, payload);
        }
    }

    private Task ProcessUnknownFrameAsync()
    {
        if (_currentHeadersStream != null)
        {
            throw CreateHeadersInterleavedException();
        }

        return Task.CompletedTask;
    }

    private Task DecodeHeadersAsync(bool endHeaders, in ReadOnlySequence<byte> payload)
    {
        Debug.Assert(_currentHeadersStream != null);

        try
        {
            _highestOpenedStreamId = _currentHeadersStream.StreamId;
            _hpackDecoder.Decode(payload, endHeaders, handler: this);

            if (endHeaders)
            {
                _currentHeadersStream.OnHeadersComplete();

                StartStream();
                ResetRequestHeaderParsingState();
            }
        }
        catch (Http2StreamErrorException)
        {
            _currentHeadersStream.Dispose();
            ResetRequestHeaderParsingState();
            throw;
        }

        return Task.CompletedTask;
    }

    private Task DecodeTrailersAsync(bool endHeaders, in ReadOnlySequence<byte> payload)
    {
        Debug.Assert(_currentHeadersStream != null);

        _hpackDecoder.Decode(payload, endHeaders, handler: this);

        if (endHeaders)
        {
            _currentHeadersStream.OnEndStreamReceived();
            ResetRequestHeaderParsingState();
        }

        return Task.CompletedTask;
    }

    private void StartStream()
    {
        Debug.Assert(_currentHeadersStream != null);

        // The stream now exists and must be tracked and drained even if Http2StreamErrorException is thrown before dispatching to the application.
        _streams[_incomingFrame.StreamId] = _currentHeadersStream;
        IncrementActiveClientStreamCount();
        _serverActiveStreamCount++;

        try
        {
            _currentHeadersStream.TotalParsedHeaderSize = _totalParsedHeaderSize;

            // This must be initialized before we offload the request or else we may start processing request body frames without it.
            _currentHeadersStream.InputRemaining = _currentHeadersStream.RequestHeaders.ContentLength;

            // This must wait until we've received all of the headers so we can verify the content-length.
            // We also must set the proper EndStream state before rejecting the request for any reason.
            if ((_headerFlags & Http2HeadersFrameFlags.END_STREAM) == Http2HeadersFrameFlags.END_STREAM)
            {
                _currentHeadersStream.OnEndStreamReceived();
            }

            if (!_isMethodConnect && (_parsedPseudoHeaderFields & _mandatoryRequestPseudoHeaderFields) != _mandatoryRequestPseudoHeaderFields)
            {
                // All HTTP/2 requests MUST include exactly one valid value for the :method, :scheme, and :path pseudo-header
                // fields, unless it is a CONNECT request (Section 8.3). An HTTP request that omits mandatory pseudo-header
                // fields is malformed (Section 8.1.2.6).
                throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.HttpErrorMissingMandatoryPseudoHeaderFields, Http2ErrorCode.PROTOCOL_ERROR);
            }

            if (_clientActiveStreamCount == _serverSettings.MaxConcurrentStreams)
            {
                // Provide feedback in server logs that the client hit the number of maximum concurrent streams,
                // and that the client is likely waiting for existing streams to be completed before it can continue.
                Log.Http2MaxConcurrentStreamsReached(_context.ConnectionId);
            }
            else if (_clientActiveStreamCount > _serverSettings.MaxConcurrentStreams)
            {
                // The protocol default stream limit is infinite so the client can exceed our limit at the start of the connection.
                // Refused streams can be retried, by which time the client must have received our settings frame with our limit information.
                throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2ErrorMaxStreams, Http2ErrorCode.REFUSED_STREAM);
            }

            // We don't use the _serverActiveRequestCount here as during shutdown, it and the dictionary counts get out of sync.
            // The streams still exist in the dictionary until the client responds with a RST or END_STREAM.
            // Also, we care about the dictionary size for too much memory consumption.
            if (_streams.Count > MaxTrackedStreams || SendEnhanceYourCalmOnStartStream)
            {
                // Server is getting hit hard with connection resets.
                // Tell client to calm down.
                // TODO consider making when to send ENHANCE_YOUR_CALM configurable?

                if (IsEnhanceYourCalmLimitEnabled && Interlocked.Increment(ref _enhanceYourCalmCount) > EnhanceYourCalmTickWindowCount * EnhanceYourCalmMaximumCount)
                {
                    Log.Http2TooManyEnhanceYourCalms(_context.ConnectionId, EnhanceYourCalmMaximumCount);

                    // Now that we've logged a useful message, we can put vague text in the exception
                    // messages in case they somehow make it back to the client (not expected)

                    // This will close the socket - we want to do that right away
                    Abort(new ConnectionAbortedException(CoreStrings.Http2ConnectionFaulted), Http2ErrorCode.ENHANCE_YOUR_CALM, ConnectionEndReason.StreamResetLimitExceeded);
                    // Throwing an exception as well will help us clean up on our end more quickly by (e.g.) skipping processing of already-buffered input
                    throw new Http2ConnectionErrorException(CoreStrings.Http2ConnectionFaulted, Http2ErrorCode.ENHANCE_YOUR_CALM, ConnectionEndReason.StreamResetLimitExceeded);
                }

                throw new Http2StreamErrorException(_currentHeadersStream.StreamId, CoreStrings.Http2TellClientToCalmDown, Http2ErrorCode.ENHANCE_YOUR_CALM);
            }
        }
        catch (Http2StreamErrorException)
        {
            MakeSpaceInDrainQueue();

            // Because this stream isn't being queued, OnRequestProcessingEnded will not be
            // automatically called and the stream won't be completed.
            // Manually complete stream to ensure pipes are completed.
            // Completing the stream will add it to the completed stream queue.
            _currentHeadersStream.DecrementActiveClientStreamCount();
            _currentHeadersStream.CompleteStream(errored: true);
            throw;
        }

        KestrelEventSource.Log.RequestQueuedStart(_currentHeadersStream, AspNetCore.Http.HttpProtocol.Http2);
        _context.ServiceContext.Metrics.RequestQueuedStart(_metricsContext, KestrelMetrics.Http2);

        // _scheduleInline is only true in tests
        if (!_scheduleInline)
        {
            // Must not allow app code to block the connection handling loop.
            ThreadPool.UnsafeQueueUserWorkItem(_currentHeadersStream, preferLocal: false);
        }
        else
        {
            _currentHeadersStream.Execute();
        }
    }

    private void ResetRequestHeaderParsingState()
    {
        _currentHeadersStream = null;
        _requestHeaderParsingState = RequestHeaderParsingState.Ready;
        _parsedPseudoHeaderFields = PseudoHeaderFields.None;
        _headerFlags = Http2HeadersFrameFlags.NONE;
        _isMethodConnect = false;
        _totalParsedHeaderSize = 0;
    }

    private void ThrowIfIncomingFrameSentToIdleStream()
    {
        // http://httpwg.org/specs/rfc7540.html#rfc.section.5.1
        // 5.1. Stream states
        // ...
        // idle:
        // ...
        // Receiving any frame other than HEADERS or PRIORITY on a stream in this state MUST be
        // treated as a connection error (Section 5.4.1) of type PROTOCOL_ERROR.
        //
        // If the stream ID in the incoming frame is higher than the highest opened stream ID so
        // far, then the incoming frame's target stream is in the idle state, which is the implicit
        // initial state for all streams.
        if (_incomingFrame.StreamId > _highestOpenedStreamId)
        {
            throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamIdle(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidStreamId);
        }
    }

    private void AbortStream(int streamId, IOException error)
    {
        if (_streams.TryGetValue(streamId, out var stream))
        {
            stream.DecrementActiveClientStreamCount();
            stream.Abort(error);
        }
    }

    void IRequestProcessor.Tick(long timestamp)
    {
        Input.CancelPendingRead();
        // We count EYCs over a window of a given length to avoid flagging short-lived bursts.
        // At the end of each window, reset the count.
        if (IsEnhanceYourCalmLimitEnabled && ++_tickCount % EnhanceYourCalmTickWindowCount == 0)
        {
            Interlocked.Exchange(ref _enhanceYourCalmCount, 0);
        }
    }

    void IHttp2StreamLifetimeHandler.OnStreamCompleted(Http2Stream stream)
    {
        _completedStreams.Enqueue(stream);
        _streamCompletionAwaitable.Complete();
    }

    private void UpdateCompletedStreams()
    {
        Http2Stream? firstRequedStream = null;
        var timestamp = TimeProvider.GetTimestamp();

        while (_completedStreams.TryDequeue(out var stream))
        {
            if (stream == firstRequedStream)
            {
                // We've checked every stream that was in _completedStreams by the time
                // _checkCompletedStreams was unset, so exit the loop.
                _completedStreams.Enqueue(stream);
                break;
            }

            if (stream.DrainExpirationTimestamp == default)
            {
                _serverActiveStreamCount--;
                stream.DrainExpirationTimestamp = TimeProvider.GetTimestamp(timestamp, Constants.RequestBodyDrainTimeout);
            }

            if (stream.EndStreamReceived || stream.RstStreamReceived || stream.DrainExpirationTimestamp < timestamp)
            {
                if (stream == _currentHeadersStream)
                {
                    // The drain expired out while receiving trailers. The most recent incoming frame is either a header or continuation frame for the timed out stream.
                    throw new Http2ConnectionErrorException(CoreStrings.FormatHttp2ErrorStreamClosed(_incomingFrame.Type, _incomingFrame.StreamId), Http2ErrorCode.STREAM_CLOSED, ConnectionEndReason.FrameAfterStreamClose);
                }

                RemoveStream(stream);
            }
            else
            {
                if (firstRequedStream == null)
                {
                    firstRequedStream = stream;
                }

                _completedStreams.Enqueue(stream);
            }
        }
    }

    private void RemoveStream(Http2Stream stream)
    {
        _streams.Remove(stream.StreamId);

        if (stream.CanReuse && StreamPool.Count < MaxStreamPoolSize)
        {
            // Pool and reuse the stream if it finished in a graceful state and there is space in the pool.

            // This property is used to remove unused streams from the pool
            stream.DrainExpirationTimestamp = TimeProvider.GetTimestamp(StreamPoolExpiry);

            StreamPool.Push(stream);
        }
        else
        {
            // Stream didn't complete gracefully or pool is full.
            stream.Dispose();
        }
    }

    // Compare to UpdateCompletedStreams, but only removes streams if over the max stream drain limit.
    private void MakeSpaceInDrainQueue()
    {
        var maxStreams = MaxTrackedStreams;
        // If we're tracking too many streams, discard the oldest.
        while (_streams.Count >= maxStreams && _completedStreams.TryDequeue(out var stream))
        {
            if (stream.DrainExpirationTimestamp == default)
            {
                _serverActiveStreamCount--;
            }

            RemoveStream(stream);
        }
    }

    private void UpdateConnectionState()
    {
        if (_isClosed != 0)
        {
            return;
        }

        if (_gracefulCloseInitiator != GracefulCloseInitiator.None && !_gracefulCloseStarted)
        {
            _gracefulCloseStarted = true;

            Log.Http2ConnectionClosing(_context.ConnectionId);

            if (_gracefulCloseInitiator == GracefulCloseInitiator.Server && _clientActiveStreamCount > 0)
            {
                _frameWriter.WriteGoAwayAsync(int.MaxValue, Http2ErrorCode.NO_ERROR).Preserve();
            }
        }

        if (_clientActiveStreamCount == 0)
        {
            if (_gracefulCloseStarted)
            {
                if (TryClose())
                {
                    SetConnectionErrorCode(_gracefulCloseReason, Http2ErrorCode.NO_ERROR);
                    _frameWriter.WriteGoAwayAsync(_highestOpenedStreamId, Http2ErrorCode.NO_ERROR).Preserve();
                }
            }
            else
            {
                if (TimeoutControl.TimerReason == TimeoutReason.None)
                {
                    TimeoutControl.SetTimeout(Limits.KeepAliveTimeout, TimeoutReason.KeepAlive);
                }

                // If we're awaiting headers, either a new stream will be started, or there will be a connection
                // error possibly due to a request header timeout, so no need to start a keep-alive timeout.
                Debug.Assert(TimeoutControl.TimerReason == TimeoutReason.RequestHeaders ||
                    TimeoutControl.TimerReason == TimeoutReason.KeepAlive);
            }
        }
    }

    public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        OnHeaderCore(HeaderType.NameAndValue, staticTableIndex: null, name, value);
    }

    public void OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        OnHeaderCore(HeaderType.Dynamic, index, name, value);
    }

    public void OnStaticIndexedHeader(int index)
    {
        Debug.Assert(index <= H2StaticTable.Count);

        ref readonly var entry = ref H2StaticTable.Get(index - 1);
        OnHeaderCore(HeaderType.Static, index, entry.Name, entry.Value);
    }

    public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
    {
        Debug.Assert(index <= H2StaticTable.Count);

        OnHeaderCore(HeaderType.StaticAndValue, index, H2StaticTable.Get(index - 1).Name, value);
    }

    private enum HeaderType
    {
        Static,
        StaticAndValue,
        Dynamic,
        NameAndValue
    }

    // We can't throw a Http2StreamErrorException here, it interrupts the header decompression state and may corrupt subsequent header frames on other streams.
    // For now these either need to be connection errors or BadRequests. If we want to downgrade any of them to stream errors later then we need to
    // rework the flow so that the remaining headers are drained and the decompression state is maintained.
    private void OnHeaderCore(HeaderType headerType, int? staticTableIndex, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        Debug.Assert(_currentHeadersStream != null);

        // https://tools.ietf.org/html/rfc7540#section-6.5.2
        // "The value is based on the uncompressed size of header fields, including the length of the name and value in octets plus an overhead of 32 octets for each header field.";
        // We don't include the 32 byte overhead hear so we can accept a little more than the advertised limit.
        _totalParsedHeaderSize += name.Length + value.Length;
        // Allow a 2x grace before aborting the connection. We'll check the size limit again later where we can send a 431.
        if (_totalParsedHeaderSize > _context.ServiceContext.ServerOptions.Limits.MaxRequestHeadersTotalSize * 2)
        {
            throw new Http2ConnectionErrorException(CoreStrings.BadRequest_HeadersExceedMaxTotalSize, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.MaxRequestHeadersTotalSizeExceeded);
        }

        try
        {
            if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
            {
                // Just use name + value bytes and do full validation for request trailers.
                // Potential performance improvement here to check for indexed headers and optimize validation.
                UpdateHeaderParsingState(value, GetPseudoHeaderField(name));
                ValidateHeaderContent(name, value);

                _currentHeadersStream.OnTrailer(name, value);
            }
            else
            {
                // Throws BadRequest for header count limit breaches.
                // Throws InvalidOperation for bad encoding.
                switch (headerType)
                {
                    case HeaderType.Static:
                        UpdateHeaderParsingState(value, GetPseudoHeaderField(staticTableIndex.GetValueOrDefault()));

                        _currentHeadersStream.OnHeader(staticTableIndex.GetValueOrDefault(), indexOnly: true, name, value);
                        break;
                    case HeaderType.StaticAndValue:
                        UpdateHeaderParsingState(value, GetPseudoHeaderField(staticTableIndex.GetValueOrDefault()));

                        // Value is new will get validated (i.e. check value doesn't contain newlines)
                        _currentHeadersStream.OnHeader(staticTableIndex.GetValueOrDefault(), indexOnly: false, name, value);
                        break;
                    case HeaderType.Dynamic:
                        // It is faster to set a header using a static table index than a name.
                        if (staticTableIndex != null)
                        {
                            UpdateHeaderParsingState(value, GetPseudoHeaderField(staticTableIndex.GetValueOrDefault()));

                            _currentHeadersStream.OnHeader(staticTableIndex.GetValueOrDefault(), indexOnly: false, name, value);
                        }
                        else
                        {
                            UpdateHeaderParsingState(value, GetPseudoHeaderField(name));

                            _currentHeadersStream.OnHeader(name, value, checkForNewlineChars: false);
                        }
                        break;
                    case HeaderType.NameAndValue:
                        UpdateHeaderParsingState(value, GetPseudoHeaderField(name));

                        // Header and value are new and will get validated (i.e. check name is lower-case, check value doesn't contain newlines)
                        ValidateHeaderContent(name, value);
                        _currentHeadersStream.OnHeader(name, value, checkForNewlineChars: true);
                        break;
                    default:
                        Debug.Fail($"Unexpected header type: {headerType}");
                        break;
                }
            }
        }
#pragma warning disable CS0618 // Type or member is obsolete
        catch (BadHttpRequestException bre) when (bre.Reason == RequestRejectionReason.TooManyHeaders)
        {
            throw new Http2ConnectionErrorException(bre.Message, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.MaxRequestHeaderCountExceeded);
        }
#pragma warning restore CS0618 // Type or member is obsolete
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException bre)
        {
            throw new Http2ConnectionErrorException(bre.Message, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidRequestHeaders);
        }
        catch (InvalidOperationException)
        {
            throw new Http2ConnectionErrorException(CoreStrings.BadRequest_MalformedRequestInvalidHeaders, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidRequestHeaders);
        }
    }

    public void OnHeadersComplete(bool endStream)
        => _currentHeadersStream!.OnHeadersComplete();

    private void ValidateHeaderContent(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        if (IsConnectionSpecificHeaderField(name, value))
        {
            throw new Http2ConnectionErrorException(CoreStrings.HttpErrorConnectionSpecificHeaderField, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidRequestHeaders);
        }

        // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2
        // A request or response containing uppercase header field names MUST be treated as malformed (Section 8.1.2.6).
        for (var i = 0; i < name.Length; i++)
        {
            if (((uint)name[i] - 65) <= (90 - 65))
            {
                if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
                {
                    throw new Http2ConnectionErrorException(CoreStrings.HttpErrorTrailerNameUppercase, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidRequestHeaders);
                }
                else
                {
                    throw new Http2ConnectionErrorException(CoreStrings.HttpErrorHeaderNameUppercase, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidRequestHeaders);
                }
            }
        }
    }

    private void UpdateHeaderParsingState(ReadOnlySpan<byte> value, PseudoHeaderFields headerField)
    {
        // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2.1
        /*
           Intermediaries that process HTTP requests or responses (i.e., any
           intermediary not acting as a tunnel) MUST NOT forward a malformed
           request or response.  Malformed requests or responses that are
           detected MUST be treated as a stream error (Section 5.4.2) of type
           PROTOCOL_ERROR.

           For malformed requests, a server MAY send an HTTP response prior to
           closing or resetting the stream.  Clients MUST NOT accept a malformed
           response.  Note that these requirements are intended to protect
           against several types of common attacks against HTTP; they are
           deliberately strict because being permissive can expose
           implementations to these vulnerabilities.*/
        if (headerField != PseudoHeaderFields.None)
        {
            if (_requestHeaderParsingState == RequestHeaderParsingState.Headers)
            {
                // All pseudo-header fields MUST appear in the header block before regular header fields.
                // Any request or response that contains a pseudo-header field that appears in a header
                // block after a regular header field MUST be treated as malformed (Section 8.1.2.6).
                throw new Http2ConnectionErrorException(CoreStrings.HttpErrorPseudoHeaderFieldAfterRegularHeaders, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidRequestHeaders);
            }

            if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
            {
                // Pseudo-header fields MUST NOT appear in trailers.
                throw new Http2ConnectionErrorException(CoreStrings.HttpErrorTrailersContainPseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidRequestHeaders);
            }

            _requestHeaderParsingState = RequestHeaderParsingState.PseudoHeaderFields;

            if (headerField == PseudoHeaderFields.Unknown)
            {
                // Endpoints MUST treat a request or response that contains undefined or invalid pseudo-header
                // fields as malformed (Section 8.1.2.6).
                throw new Http2ConnectionErrorException(CoreStrings.HttpErrorUnknownPseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidRequestHeaders);
            }

            if (headerField == PseudoHeaderFields.Status)
            {
                // Pseudo-header fields defined for requests MUST NOT appear in responses; pseudo-header fields
                // defined for responses MUST NOT appear in requests.
                throw new Http2ConnectionErrorException(CoreStrings.HttpErrorResponsePseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidRequestHeaders);
            }

            if ((_parsedPseudoHeaderFields & headerField) == headerField)
            {
                // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2.3
                // All HTTP/2 requests MUST include exactly one valid value for the :method, :scheme, and :path pseudo-header fields
                throw new Http2ConnectionErrorException(CoreStrings.HttpErrorDuplicatePseudoHeaderField, Http2ErrorCode.PROTOCOL_ERROR, ConnectionEndReason.InvalidRequestHeaders);
            }

            if (headerField == PseudoHeaderFields.Method)
            {
                _isMethodConnect = value.SequenceEqual(ConnectBytes);
            }

            _parsedPseudoHeaderFields |= headerField;
        }
        else if (_requestHeaderParsingState != RequestHeaderParsingState.Trailers)
        {
            _requestHeaderParsingState = RequestHeaderParsingState.Headers;
        }
    }

    private static PseudoHeaderFields GetPseudoHeaderField(int staticTableIndex)
    {
        Debug.Assert(staticTableIndex > 0, "Static table starts at 1.");

        var headerField = staticTableIndex switch
        {
            1 => PseudoHeaderFields.Authority,
            2 => PseudoHeaderFields.Method,
            3 => PseudoHeaderFields.Method,
            4 => PseudoHeaderFields.Path,
            5 => PseudoHeaderFields.Path,
            6 => PseudoHeaderFields.Scheme,
            7 => PseudoHeaderFields.Scheme,
            8 => PseudoHeaderFields.Status,
            9 => PseudoHeaderFields.Status,
            10 => PseudoHeaderFields.Status,
            11 => PseudoHeaderFields.Status,
            12 => PseudoHeaderFields.Status,
            13 => PseudoHeaderFields.Status,
            14 => PseudoHeaderFields.Status,
            _ => PseudoHeaderFields.None
        };

        return headerField;
    }

    private static PseudoHeaderFields GetPseudoHeaderField(ReadOnlySpan<byte> name)
    {
        if (name.IsEmpty || name[0] != (byte)':')
        {
            return PseudoHeaderFields.None;
        }
        else if (name.SequenceEqual(PathBytes))
        {
            return PseudoHeaderFields.Path;
        }
        else if (name.SequenceEqual(MethodBytes))
        {
            return PseudoHeaderFields.Method;
        }
        else if (name.SequenceEqual(SchemeBytes))
        {
            return PseudoHeaderFields.Scheme;
        }
        else if (name.SequenceEqual(StatusBytes))
        {
            return PseudoHeaderFields.Status;
        }
        else if (name.SequenceEqual(AuthorityBytes))
        {
            return PseudoHeaderFields.Authority;
        }
        else if (name.SequenceEqual(ProtocolBytes))
        {
            return PseudoHeaderFields.Protocol;
        }
        else
        {
            return PseudoHeaderFields.Unknown;
        }
    }

    private static bool IsConnectionSpecificHeaderField(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        return name.SequenceEqual(ConnectionBytes) || (name.SequenceEqual(TeBytes) && !value.SequenceEqual(TrailersBytes));
    }

    private bool TryClose()
    {
        if (Interlocked.Exchange(ref _isClosed, 1) == 0)
        {
            Log.Http2ConnectionClosed(_context.ConnectionId, _highestOpenedStreamId);
            return true;
        }

        return false;
    }

    public void IncrementActiveClientStreamCount()
    {
        Interlocked.Increment(ref _clientActiveStreamCount);
    }

    public void DecrementActiveClientStreamCount()
    {
        Interlocked.Decrement(ref _clientActiveStreamCount);
    }

    private PipeOptions GetInputPipeOptions() => new PipeOptions(pool: _context.MemoryPool,
            readerScheduler: _context.ServiceContext.Scheduler,
            writerScheduler: PipeScheduler.Inline,
            pauseWriterThreshold: 1,
            resumeWriterThreshold: 1,
            minimumSegmentSize: _context.MemoryPool.GetMinimumSegmentSize(),
            useSynchronizationContext: false);

    private async Task CopyPipeAsync(PipeReader reader, PipeWriter writer)
    {
        Exception? error = null;
        try
        {
            while (true)
            {
                var readResult = await reader.ReadAsync();

                if ((readResult.IsCompleted && readResult.Buffer.Length == 0) || readResult.IsCanceled)
                {
                    // FIN
                    break;
                }

                var outputBuffer = writer.GetMemory(_minAllocBufferSize);

                var copyAmount = (int)Math.Min(outputBuffer.Length, readResult.Buffer.Length);
                var bufferSlice = readResult.Buffer.Slice(0, copyAmount);

                bufferSlice.CopyTo(outputBuffer.Span);

                writer.Advance(copyAmount);

                var result = await writer.FlushAsync();

                reader.AdvanceTo(bufferSlice.End);

                if (result.IsCompleted || result.IsCanceled)
                {
                    // flushResult should not be canceled.
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            // Don't rethrow the exception. It should be handled by the Pipeline consumer.
            error = ex;
        }
        finally
        {
            await reader.CompleteAsync(error);
            await writer.CompleteAsync(error);
        }
    }

    private enum RequestHeaderParsingState
    {
        Ready,
        PseudoHeaderFields,
        Headers,
        Trailers
    }

    [Flags]
    private enum PseudoHeaderFields
    {
        None = 0x0,
        Authority = 0x1,
        Method = 0x2,
        Path = 0x4,
        Scheme = 0x8,
        Status = 0x10,
        Protocol = 0x20,
        Unknown = 0x40000000
    }

    private static class GracefulCloseInitiator
    {
        public const int None = 0;
        public const int Server = 1;
        public const int Client = 2;
    }
}
