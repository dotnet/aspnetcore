// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.QPack;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using HttpCharacters = Microsoft.AspNetCore.Http.HttpCharacters;
using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;
using HttpMethods = Microsoft.AspNetCore.Http.HttpMethods;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal abstract partial class Http3Stream : HttpProtocol, IHttp3Stream, IHttpStreamHeadersHandler, IThreadPoolWorkItem
{
    private static ReadOnlySpan<byte> AuthorityBytes => ":authority"u8;
    private static ReadOnlySpan<byte> MethodBytes => ":method"u8;
    private static ReadOnlySpan<byte> PathBytes => ":path"u8;
    private static ReadOnlySpan<byte> ProtocolBytes => ":protocol"u8;
    private static ReadOnlySpan<byte> SchemeBytes => ":scheme"u8;
    private static ReadOnlySpan<byte> StatusBytes => ":status"u8;
    private static ReadOnlySpan<byte> ConnectionBytes => "connection"u8;
    private static ReadOnlySpan<byte> TeBytes => "te"u8;
    private static ReadOnlySpan<byte> TrailersBytes => "trailers"u8;
    private static ReadOnlySpan<byte> ConnectBytes => "CONNECT"u8;

    private const PseudoHeaderFields _mandatoryRequestPseudoHeaderFields =
        PseudoHeaderFields.Method | PseudoHeaderFields.Path | PseudoHeaderFields.Scheme;

    private Http3FrameWriter _frameWriter = default!;
    private Http3OutputProducer _http3Output = default!;
    private Http3StreamContext _context = default!;
    private IProtocolErrorCodeFeature _errorCodeFeature = default!;
    private IStreamIdFeature _streamIdFeature = default!;
    private IStreamAbortFeature _streamAbortFeature = default!;
    private IStreamClosedFeature _streamClosedFeature = default!;
    private PseudoHeaderFields _parsedPseudoHeaderFields;
    private StreamCompletionFlags _completionState;
    private int _isClosed;
    private int _totalParsedHeaderSize;
    private bool _isMethodConnect;
    private bool _isWebTransportSessionAccepted;
    private Http3MessageBody? _messageBody;

    private readonly ManualResetValueTaskSource<object?> _appCompletedTaskSource = new();
    private readonly Lock _completionLock = new();

    protected RequestHeaderParsingState _requestHeaderParsingState;
    protected readonly Http3RawFrame _incomingFrame = new();

    public bool EndStreamReceived => (_completionState & StreamCompletionFlags.EndStreamReceived) == StreamCompletionFlags.EndStreamReceived;
    public bool IsAborted => (_completionState & StreamCompletionFlags.Aborted) == StreamCompletionFlags.Aborted;
    private bool IsAbortedRead => (_completionState & StreamCompletionFlags.AbortedRead) == StreamCompletionFlags.AbortedRead;
    public bool IsCompleted => (_completionState & StreamCompletionFlags.Completed) == StreamCompletionFlags.Completed;

    public Pipe RequestBodyPipe { get; private set; } = default!;
    public long? InputRemaining { get; internal set; }
    public QPackDecoder QPackDecoder { get; private set; } = default!;

    public PipeReader Input => _context.Transport.Input;
    public KestrelServerLimits Limits => _context.ServiceContext.ServerOptions.Limits;
    public long StreamId => _streamIdFeature.StreamId;
    public long StreamTimeoutTimestamp { get; set; }
    public bool IsReceivingHeader => _requestHeaderParsingState <= RequestHeaderParsingState.Headers; // Assigned once headers are received
    public bool IsDraining => _appCompletedTaskSource.GetStatus() != ValueTaskSourceStatus.Pending; // Draining starts once app is complete
    public bool IsRequestStream => true;
    public BaseConnectionContext ConnectionContext => _context.ConnectionContext;
    public ConnectionMetricsContext MetricsContext => _context.MetricsContext;

    public void Initialize(Http3StreamContext context)
    {
        base.Initialize(context);

        InputRemaining = null;

        _context = context;

        _errorCodeFeature = _context.ConnectionFeatures.GetRequiredFeature<IProtocolErrorCodeFeature>();
        _streamIdFeature = _context.ConnectionFeatures.GetRequiredFeature<IStreamIdFeature>();
        _streamAbortFeature = _context.ConnectionFeatures.GetRequiredFeature<IStreamAbortFeature>();
        _streamClosedFeature = _context.ConnectionFeatures.GetRequiredFeature<IStreamClosedFeature>();

        _appCompletedTaskSource.Reset();
        _isClosed = 0;
        _requestHeaderParsingState = default;
        _parsedPseudoHeaderFields = default;
        _totalParsedHeaderSize = 0;
        // Allow up to 2x during parsing, enforce the hard limit after when we can preserve the connection.
        _eagerRequestHeadersParsedLimit = ServerOptions.Limits.MaxRequestHeaderCount * 2;
        _isMethodConnect = false;
        _completionState = default;
        StreamTimeoutTimestamp = 0;

        if (_frameWriter == null)
        {
            _frameWriter = new Http3FrameWriter(
                context.StreamContext,
                context.TimeoutControl,
                context.ServiceContext.ServerOptions.Limits.MinResponseDataRate,
                context.MemoryPool,
                context.ServiceContext.Log,
                _streamIdFeature,
                context.ClientPeerSettings,
                this);

            _http3Output = new Http3OutputProducer(
                _frameWriter,
                context.MemoryPool,
                this,
                context.ServiceContext.Log);
            Output = _http3Output;
            RequestBodyPipe = CreateRequestBodyPipe(64 * 1024); // windowSize?
            QPackDecoder = new QPackDecoder(_context.ServiceContext.ServerOptions.Limits.Http3.MaxRequestHeaderFieldSize);
        }
        else
        {
            _http3Output.StreamReset();
            RequestBodyPipe.Reset();
            QPackDecoder.Reset();
        }

        _frameWriter.Reset(context.Transport.Output, context.ConnectionId);
    }

    public void InitializeWithExistingContext(IDuplexPipe transport)
    {
        _context.Transport = transport;
        Initialize(_context);
    }

    public void Abort(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
    {
        AbortCore(abortReason, errorCode);
    }

    private void AbortCore(Exception exception, Http3ErrorCode errorCode)
    {
        lock (_completionLock)
        {
            if (IsCompleted || IsAborted)
            {
                return;
            }

            var (oldState, newState) = ApplyCompletionFlag(StreamCompletionFlags.Aborted);

            if (oldState == newState)
            {
                return;
            }

            if (!(exception is ConnectionAbortedException abortReason))
            {
                abortReason = new ConnectionAbortedException(exception.Message, exception);
            }

            // This has the side-effect of validating the error code, so do it before we consume the error code
            _errorCodeFeature.Error = (long)errorCode;

            _context.WebTransportSession?.Abort(abortReason, errorCode);

            Log.Http3StreamAbort(TraceIdentifier, errorCode, abortReason);

            // Call _http3Output.Stop() prior to poisoning the request body stream or pipe to
            // ensure that an app that completes early due to the abort doesn't result in header frames being sent.
            _http3Output.Stop();

            CancelRequestAbortedToken();

            // Unblock the request body.
            PoisonBody(exception);
            RequestBodyPipe.Writer.Complete(exception);

            // Abort framewriter and underlying transport after stopping output.
            _frameWriter.Abort(abortReason);
        }
    }

    protected override void OnErrorAfterResponseStarted()
    {
        // We can no longer change the response, send a Reset instead.
        var abortReason = new ConnectionAbortedException(CoreStrings.Http3StreamErrorAfterHeaders);
        Abort(abortReason, Http3ErrorCode.InternalError);
    }

    public void OnHeadersComplete(bool endStream)
    {
        OnHeadersComplete();
    }

    public void OnStaticIndexedHeader(int index)
    {
        Debug.Assert(index <= H3StaticTable.Count);

        ref readonly var entry = ref H3StaticTable.Get(index);
        OnHeaderCore(HeaderType.Static, index, entry.Name, entry.Value);
    }

    public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
    {
        Debug.Assert(index <= H3StaticTable.Count);

        OnHeaderCore(HeaderType.StaticAndValue, index, H3StaticTable.Get(index).Name, value);
    }

    public void OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        OnHeaderCore(HeaderType.Dynamic, index, name, value);
    }

    public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        OnHeaderCore(HeaderType.NameAndValue, staticTableIndex: null, name, value);
    }

    private enum HeaderType
    {
        Static,
        StaticAndValue,
        Dynamic,
        NameAndValue
    }

    public override void OnHeader(int index, bool indexOnly, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        base.OnHeader(index, indexOnly, name, value);

        if (indexOnly)
        {
            // Special case setting headers when the value is indexed for performance.
            switch (index)
            {
                case H3StaticTable.MethodGet:
                    HttpRequestHeaders.HeaderMethod = HttpMethods.Get;
                    Method = HttpMethod.Get;
                    _methodText = HttpMethods.Get;
                    return;
                case H3StaticTable.MethodPost:
                    HttpRequestHeaders.HeaderMethod = HttpMethods.Post;
                    Method = HttpMethod.Post;
                    _methodText = HttpMethods.Post;
                    return;
                case H3StaticTable.SchemeHttp:
                    HttpRequestHeaders.HeaderScheme = SchemeHttp;
                    return;
                case H3StaticTable.SchemeHttps:
                    HttpRequestHeaders.HeaderScheme = SchemeHttps;
                    return;
            }
        }

        // QPack append will return false if the index is not a known request header.
        // For example, someone could send the index of "Server" (a response header) in the request.
        // If that happens then fallback to using Append with the name bytes.
        //
        // If the value is indexed then we know it doesn't contain new lines and can skip checking.
        if (!HttpRequestHeaders.TryQPackAppend(index, value, checkForNewlineChars: !indexOnly))
        {
            AppendHeader(name, value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        HttpRequestHeaders.Append(name, value, checkForNewlineChars: true);
    }

    private void OnHeaderCore(HeaderType headerType, int? staticTableIndex, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        // https://httpwg.org/specs/rfc9114.html#rfc.section.4.2.2
        // "The value is based on the uncompressed size of header fields, including the length of the name and value in octets plus an overhead of 32 octets for each header field.";
        // We don't include the 32 byte overhead hear so we can accept a little more than the advertised limit.
        _totalParsedHeaderSize += name.Length + value.Length;
        // Allow a 2x grace before aborting the stream. We'll check the size limit again later where we can send a 431.
        if (_totalParsedHeaderSize > ServerOptions.Limits.MaxRequestHeadersTotalSize * 2)
        {
            throw new Http3StreamErrorException(CoreStrings.BadRequest_HeadersExceedMaxTotalSize, Http3ErrorCode.RequestRejected);
        }

        try
        {
            if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
            {
                // Just use name + value bytes and do full validation for request trailers.
                // Potential performance improvement here to check for indexed headers and optimize validation.
                UpdateHeaderParsingState(value, GetPseudoHeaderField(name));
                ValidateHeaderContent(name, value);

                OnTrailer(name, value);
            }
            else
            {
                // Throws BadRequest for header count limit breaches.
                // Throws InvalidOperation for bad encoding.
                switch (headerType)
                {
                    case HeaderType.Static:
                        UpdateHeaderParsingState(value, GetPseudoHeaderField(staticTableIndex.GetValueOrDefault()));

                        OnHeader(staticTableIndex.GetValueOrDefault(), indexOnly: true, name, value);
                        break;
                    case HeaderType.StaticAndValue:
                        UpdateHeaderParsingState(value, GetPseudoHeaderField(staticTableIndex.GetValueOrDefault()));

                        // Value is new will get validated (i.e. check value doesn't contain newlines)
                        OnHeader(staticTableIndex.GetValueOrDefault(), indexOnly: false, name, value);
                        break;
                    case HeaderType.Dynamic:
                        // It is faster to set a header using a static table index than a name.
                        if (staticTableIndex != null)
                        {
                            UpdateHeaderParsingState(value, GetPseudoHeaderField(staticTableIndex.GetValueOrDefault()));

                            OnHeader(staticTableIndex.GetValueOrDefault(), indexOnly: false, name, value);
                        }
                        else
                        {
                            UpdateHeaderParsingState(value, GetPseudoHeaderField(name));

                            OnHeader(name, value, checkForNewlineChars: false);
                        }
                        break;
                    case HeaderType.NameAndValue:
                        UpdateHeaderParsingState(value, GetPseudoHeaderField(name));

                        // Header and value are new and will get validated (i.e. check name is lower-case, check value doesn't contain newlines)
                        ValidateHeaderContent(name, value);
                        OnHeader(name, value, checkForNewlineChars: true);
                        break;
                    default:
                        Debug.Fail($"Unexpected header type: {headerType}");
                        break;
                }
            }
        }
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException bre)
        {
            throw new Http3StreamErrorException(bre.Message, Http3ErrorCode.MessageError);
        }
        catch (InvalidOperationException)
        {
            throw new Http3StreamErrorException(CoreStrings.BadRequest_MalformedRequestInvalidHeaders, Http3ErrorCode.MessageError);
        }
    }

    private void ValidateHeaderContent(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        if (IsConnectionSpecificHeaderField(name, value))
        {
            throw new Http3StreamErrorException(CoreStrings.HttpErrorConnectionSpecificHeaderField, Http3ErrorCode.MessageError);
        }

        // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2
        // A request or response containing uppercase header field names MUST be treated as malformed (Section 8.1.2.6).
        for (var i = 0; i < name.Length; i++)
        {
            if (((uint)name[i] - 65) <= (90 - 65))
            {
                if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
                {
                    throw new Http3StreamErrorException(CoreStrings.HttpErrorTrailerNameUppercase, Http3ErrorCode.MessageError);
                }
                else
                {
                    throw new Http3StreamErrorException(CoreStrings.HttpErrorHeaderNameUppercase, Http3ErrorCode.MessageError);
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
                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.1-4
                // All pseudo-header fields MUST appear in the header block before regular header fields.
                // Any request or response that contains a pseudo-header field that appears in a header
                // block after a regular header field MUST be treated as malformed (Section 8.1.2.6).
                throw new Http3StreamErrorException(CoreStrings.HttpErrorPseudoHeaderFieldAfterRegularHeaders, Http3ErrorCode.MessageError);
            }

            if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
            {
                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.1-3
                // Pseudo-header fields MUST NOT appear in trailers.
                throw new Http3StreamErrorException(CoreStrings.HttpErrorTrailersContainPseudoHeaderField, Http3ErrorCode.MessageError);
            }

            _requestHeaderParsingState = RequestHeaderParsingState.PseudoHeaderFields;

            if (headerField == PseudoHeaderFields.Unknown)
            {
                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.1-3
                // Endpoints MUST treat a request or response that contains undefined or invalid pseudo-header
                // fields as malformed.
                throw new Http3StreamErrorException(CoreStrings.HttpErrorUnknownPseudoHeaderField, Http3ErrorCode.MessageError);
            }

            if (headerField == PseudoHeaderFields.Status)
            {
                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.1-3
                // Pseudo-header fields defined for requests MUST NOT appear in responses; pseudo-header fields
                // defined for responses MUST NOT appear in requests.
                throw new Http3StreamErrorException(CoreStrings.HttpErrorResponsePseudoHeaderField, Http3ErrorCode.MessageError);
            }

            if ((_parsedPseudoHeaderFields & headerField) == headerField)
            {
                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.1-7
                // All HTTP/3 requests MUST include exactly one valid value for the :method, :scheme, and :path pseudo-header fields
                throw new Http3StreamErrorException(CoreStrings.HttpErrorDuplicatePseudoHeaderField, Http3ErrorCode.MessageError);
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
        Debug.Assert(staticTableIndex >= 0, "Static table starts at 0.");

        var headerField = staticTableIndex switch
        {
            0 => PseudoHeaderFields.Authority,
            1 => PseudoHeaderFields.Path,
            15 => PseudoHeaderFields.Method,
            16 => PseudoHeaderFields.Method,
            17 => PseudoHeaderFields.Method,
            18 => PseudoHeaderFields.Method,
            19 => PseudoHeaderFields.Method,
            20 => PseudoHeaderFields.Method,
            21 => PseudoHeaderFields.Method,
            22 => PseudoHeaderFields.Scheme,
            23 => PseudoHeaderFields.Scheme,
            24 => PseudoHeaderFields.Status,
            25 => PseudoHeaderFields.Status,
            26 => PseudoHeaderFields.Status,
            27 => PseudoHeaderFields.Status,
            28 => PseudoHeaderFields.Status,
            63 => PseudoHeaderFields.Status,
            64 => PseudoHeaderFields.Status,
            65 => PseudoHeaderFields.Status,
            66 => PseudoHeaderFields.Status,
            67 => PseudoHeaderFields.Status,
            68 => PseudoHeaderFields.Status,
            69 => PseudoHeaderFields.Status,
            70 => PseudoHeaderFields.Status,
            71 => PseudoHeaderFields.Status,
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

    protected override void OnRequestProcessingEnded()
    {
        CompleteStream(errored: false);
    }

    private void CompleteStream(bool errored)
    {
        if (!EndStreamReceived)
        {
            if (!errored)
            {
                Log.RequestBodyNotEntirelyRead(ConnectionIdFeature, TraceIdentifier);
            }

            var (oldState, newState) = ApplyCompletionFlag(StreamCompletionFlags.AbortedRead);
            if (oldState != newState)
            {
                // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1-15
                // When the server does not need to receive the remainder of the request, it MAY abort reading
                // the request stream, send a complete response, and cleanly close the sending part of the stream.
                // The error code H3_NO_ERROR SHOULD be used when requesting that the client stop sending on the
                // request stream.
                _streamAbortFeature.AbortRead((long)Http3ErrorCode.NoError, new ConnectionAbortedException("The application completed without reading the entire request body."));
                RequestBodyPipe.Writer.Complete();
            }

            TryClose();
        }

        _http3Output.Complete();

        // Stream will be pooled after app completed.
        // Wait to signal app completed after any potential aborts on the stream.
        _appCompletedTaskSource.SetResult(null);
    }

    private bool TryClose()
    {
        if (Interlocked.Exchange(ref _isClosed, 1) == 0)
        {
            // Wake ProcessRequestAsync loop so that it can exit.
            Input.CancelPendingRead();

            return true;
        }

        return false;
    }

    public async Task ProcessRequestAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        Exception? error = null;

        // With HTTP/3 the write-side of the stream can be aborted by the client after the server
        // has finished reading incoming content. That means errors can happen after the Input loop
        // has finished reading.
        //
        // To get notification of request aborted we register to the stream closing or complete.
        // It will notify this type that the client has aborted the request and Kestrel will complete
        // pipes and cancel the HttpContext.RequestAborted token.
        _streamClosedFeature.OnClosed(static s =>
        {
            var stream = (Http3Stream)s!;

            if (!stream.IsCompleted)
            {
                // An error code value other than -1 indicates a value was set and the request didn't gracefully complete.
                var errorCode = stream._errorCodeFeature.Error;
                if (errorCode >= 0)
                {
                    stream.AbortCore(new IOException(CoreStrings.HttpStreamResetByClient), (Http3ErrorCode)errorCode);
                }
            }
        }, this);

        try
        {
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
                        while (Http3FrameReader.TryReadFrame(ref readableBuffer, _incomingFrame, out var framePayload))
                        {
                            Log.Http3FrameReceived(ConnectionId, _streamIdFeature.StreamId, _incomingFrame);

                            consumed = examined = framePayload.End;
                            await ProcessHttp3Stream(application, framePayload, result.IsCompleted && readableBuffer.IsEmpty);
                        }
                    }

                    if (result.IsCompleted)
                    {
                        await OnEndStreamReceived();
                        return;
                    }
                }
                finally
                {
                    Input.AdvanceTo(consumed, examined);
                }
            }
        }
        catch (Http3StreamErrorException ex)
        {
            error = ex;
            Abort(new ConnectionAbortedException(ex.Message, ex), ex.ErrorCode);
        }
        catch (Http3ConnectionErrorException ex)
        {
            error = ex;
            _errorCodeFeature.Error = (long)ex.ErrorCode;

            _context.StreamLifetimeHandler.OnStreamConnectionError(ex);
        }
        catch (ConnectionAbortedException ex)
        {
            error = ex;
        }
        catch (ConnectionResetException ex)
        {
            error = ex;
            var resolvedErrorCode = _errorCodeFeature.Error >= 0 ? _errorCodeFeature.Error : 0;
            AbortCore(new IOException(CoreStrings.HttpStreamResetByClient, ex), (Http3ErrorCode)resolvedErrorCode);
        }
        catch (Exception ex)
        {
            error = ex;
            Log.LogWarning(0, ex, "Stream threw an unexpected exception.");
        }
        finally
        {
            await Input.CompleteAsync();

            // Once the header is finished being received then the app has started.
            var appCompletedTask = !IsReceivingHeader
                ? new ValueTask(_appCompletedTaskSource, _appCompletedTaskSource.Version)
                : ValueTask.CompletedTask;

            // At this point, assuming an error wasn't thrown, the stream's read-side is complete.
            // Make sure application func is completed before completing writer.
            await appCompletedTask;

            try
            {
                await _frameWriter.CompleteAsync();
            }
            catch
            {
                var streamError = error as ConnectionAbortedException
                   ?? new ConnectionAbortedException("The stream has completed.", error!);

                Abort(streamError, Http3ErrorCode.ProtocolError);
                throw;
            }
            finally
            {
                // Drain transports and dispose.
                await _context.StreamContext.DisposeAsync();

                // Tells the connection to remove the stream from its active collection.
                ApplyCompletionFlag(StreamCompletionFlags.Completed);
                _context.StreamLifetimeHandler.OnStreamCompleted(this);

                // If we have a webtransport session on this stream, end it
                _context.WebTransportSession?.OnClientConnectionClosed();

                // TODO this is a hack for .NET 6 pooling.
                //
                // Pooling needs to happen after transports have been drained and stream
                // has been completed and is no longer active. All of this logic can't
                // be placed in ConnectionContext.DisposeAsync. Instead, QuicStreamContext
                // has pooling happen in QuicStreamContext.Dispose.
                //
                // ConnectionContext only implements IDisposableAsync by default. Only
                // QuicStreamContext should pass this check.
                if (_context.StreamContext is IDisposable disposableStream)
                {
                    disposableStream.Dispose();
                }
            }
        }
    }

    private ValueTask OnEndStreamReceived()
    {
        ApplyCompletionFlag(StreamCompletionFlags.EndStreamReceived);

        if (_requestHeaderParsingState == RequestHeaderParsingState.Ready)
        {
            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1-14
            // Request stream ended without headers received. Unable to provide response.
            throw new Http3StreamErrorException(CoreStrings.Http3StreamErrorRequestEndedNoHeaders, Http3ErrorCode.RequestIncomplete);
        }

        if (InputRemaining.HasValue)
        {
            // https://tools.ietf.org/html/rfc7540#section-8.1.2.6
            if (InputRemaining.Value != 0)
            {
                throw new Http3StreamErrorException(CoreStrings.Http3StreamErrorLessDataThanLength, Http3ErrorCode.ProtocolError);
            }
        }

        _context.WebTransportSession?.OnClientConnectionClosed();

        OnTrailersComplete();
        return RequestBodyPipe.Writer.CompleteAsync();
    }

    private Task ProcessHttp3Stream<TContext>(IHttpApplication<TContext> application, in ReadOnlySequence<byte> payload, bool isCompleted) where TContext : notnull
    {
        return _incomingFrame.Type switch
        {
            Http3FrameType.Data => ProcessDataFrameAsync(payload),
            Http3FrameType.Headers => ProcessHeadersFrameAsync(application, payload, isCompleted),
            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-7.2.4
            // These frames need to be on a control stream
            Http3FrameType.Settings or
            Http3FrameType.CancelPush or
            Http3FrameType.GoAway or
            Http3FrameType.MaxPushId => throw new Http3ConnectionErrorException(
                CoreStrings.FormatHttp3ErrorUnsupportedFrameOnRequestStream(_incomingFrame.FormattedType), Http3ErrorCode.UnexpectedFrame, ConnectionEndReason.UnexpectedFrame),
            // The server should never receive push promise
            Http3FrameType.PushPromise => throw new Http3ConnectionErrorException(
                CoreStrings.FormatHttp3ErrorUnsupportedFrameOnServer(_incomingFrame.FormattedType), Http3ErrorCode.UnexpectedFrame, ConnectionEndReason.UnexpectedFrame),
            _ => ProcessUnknownFrameAsync(),
        };
    }

    private static Task ProcessUnknownFrameAsync()
    {
        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-9
        // Unknown frames must be explicitly ignored.
        return Task.CompletedTask;
    }

    private async Task ProcessHeadersFrameAsync<TContext>(IHttpApplication<TContext> application, ReadOnlySequence<byte> payload, bool isCompleted) where TContext : notnull
    {
        // HEADERS frame after trailing headers is invalid.
        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1
        if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
        {
            throw new Http3ConnectionErrorException(CoreStrings.FormatHttp3StreamErrorFrameReceivedAfterTrailers(Http3Formatting.ToFormattedType(Http3FrameType.Headers)), Http3ErrorCode.UnexpectedFrame, ConnectionEndReason.UnexpectedFrame);
        }

        if (_requestHeaderParsingState == RequestHeaderParsingState.Body)
        {
            _requestHeaderParsingState = RequestHeaderParsingState.Trailers;
        }

        try
        {
            QPackDecoder.Decode(payload, endHeaders: true, handler: this);
            QPackDecoder.Reset();
        }
        catch (QPackDecodingException ex)
        {
            Log.QPackDecodingError(ConnectionId, StreamId, ex);
            throw new Http3StreamErrorException(ex.Message, Http3ErrorCode.InternalError);
        }

        switch (_requestHeaderParsingState)
        {
            case RequestHeaderParsingState.Ready:
            case RequestHeaderParsingState.PseudoHeaderFields:
                _requestHeaderParsingState = RequestHeaderParsingState.Headers;
                break;
            case RequestHeaderParsingState.Headers:
                break;
            case RequestHeaderParsingState.Trailers:
                return;
            default:
                Debug.Fail("Unexpected header parsing state.");
                break;
        }

        InputRemaining = HttpRequestHeaders.ContentLength;

        OnHeadersComplete();

        // If the stream is complete after receiving the headers then run OnEndStreamReceived.
        // If there is a bad content length then this will throw before the request delegate is called.
        if (isCompleted)
        {
            await OnEndStreamReceived();
        }

        // https://datatracker.ietf.org/doc/html/draft-ietf-webtrans-http3/#section-3.3
        if (_context.ServiceContext.ServerOptions.EnableWebTransportAndH3Datagrams && HttpRequestHeaders.HeaderProtocol.Count > 0)
        {
            if (!_isMethodConnect)
            {
                throw new Http3StreamErrorException(CoreStrings.Http3MethodMustBeConnectWhenUsingProtocolPseudoHeader, Http3ErrorCode.ProtocolError);
            }

            if (!_parsedPseudoHeaderFields.HasFlag(PseudoHeaderFields.Authority) || !_parsedPseudoHeaderFields.HasFlag(PseudoHeaderFields.Path))
            {
                throw new Http3StreamErrorException(CoreStrings.Http3MissingAuthorityOrPathPseudoHeaders, Http3ErrorCode.ProtocolError);
            }

            if (_context.ClientPeerSettings.EnableWebTransport != _context.ServerPeerSettings.EnableWebTransport)
            {
                throw new Http3StreamErrorException(CoreStrings.FormatHttp3WebTransportStatusMismatch(_context.ClientPeerSettings.EnableWebTransport == 1, _context.ServerPeerSettings.EnableWebTransport == 1), Http3ErrorCode.SettingsError);
            }

            if (_context.ClientPeerSettings.H3Datagram != _context.ServerPeerSettings.H3Datagram)
            {
                throw new Http3StreamErrorException(CoreStrings.FormatHttp3DatagramStatusMismatch(_context.ClientPeerSettings.H3Datagram == 1, _context.ServerPeerSettings.H3Datagram == 1), Http3ErrorCode.SettingsError);
            }

            if (string.Equals(HttpRequestHeaders.HeaderProtocol, WebTransportSession.WebTransportProtocolValue, StringComparison.Ordinal))
            {
                // if the client supports the same version of WebTransport as Kestrel, make this a WebTransport request
                if (((AspNetCore.Http.IHeaderDictionary)HttpRequestHeaders).TryGetValue(WebTransportSession.CurrentSupportedVersion, out var version) && string.Equals(version, WebTransportSession.VersionEnabledIndicator, StringComparison.Ordinal))
                {
                    IsWebTransportRequest = true;
                }
            }
        }
        else if (!_isMethodConnect && (_parsedPseudoHeaderFields & _mandatoryRequestPseudoHeaderFields) != _mandatoryRequestPseudoHeaderFields)
        {
            // All HTTP/3 requests MUST include exactly one valid value for the :method, :scheme, and :path pseudo-header
            // fields, unless it is a CONNECT request. An HTTP request that omits mandatory pseudo-header
            // fields is malformed.
            // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1.1.1
            throw new Http3StreamErrorException(CoreStrings.HttpErrorMissingMandatoryPseudoHeaderFields, Http3ErrorCode.MessageError);
        }

        _requestHeaderParsingState = RequestHeaderParsingState.Body;
        StreamTimeoutTimestamp = default;
        _context.StreamLifetimeHandler.OnStreamHeaderReceived(this);

        ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
    }

    private Task ProcessDataFrameAsync(in ReadOnlySequence<byte> payload)
    {
        // DATA frame before headers is invalid.
        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1
        if (_requestHeaderParsingState == RequestHeaderParsingState.Ready)
        {
            throw new Http3ConnectionErrorException(CoreStrings.Http3StreamErrorDataReceivedBeforeHeaders, Http3ErrorCode.UnexpectedFrame, ConnectionEndReason.UnexpectedFrame);
        }

        // DATA frame after trailing headers is invalid.
        // https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1
        if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
        {
            var message = CoreStrings.FormatHttp3StreamErrorFrameReceivedAfterTrailers(Http3Formatting.ToFormattedType(Http3FrameType.Data));
            throw new Http3ConnectionErrorException(message, Http3ErrorCode.UnexpectedFrame, ConnectionEndReason.UnexpectedFrame);
        }

        if (InputRemaining.HasValue)
        {
            // https://tools.ietf.org/html/rfc7540#section-8.1.2.6
            if (payload.Length > InputRemaining.Value)
            {
                throw new Http3StreamErrorException(CoreStrings.Http3StreamErrorMoreDataThanLength, Http3ErrorCode.ProtocolError);
            }

            InputRemaining -= payload.Length;
        }

        lock (_completionLock)
        {
            if (IsAborted || IsAbortedRead)
            {
                return Task.CompletedTask;
            }

            foreach (var segment in payload)
            {
                RequestBodyPipe.Writer.Write(segment.Span);
            }

            return RequestBodyPipe.Writer.FlushAsync().GetAsTask();
        }
    }

    protected override void OnReset()
    {
        _keepAlive = true;
        _connectionAborted = false;
        _userTrailers = null;
        _isWebTransportSessionAccepted = false;
        _isMethodConnect = false;

        // Reset Http3 Features
        _currentIHttpMinRequestBodyDataRateFeature = this;
        _currentIHttpResponseTrailersFeature = this;
        _currentIHttpResetFeature = this;
    }

    protected override void ApplicationAbort() => ApplicationAbort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication), Http3ErrorCode.InternalError);

    private void ApplicationAbort(ConnectionAbortedException abortReason, Http3ErrorCode error)
    {
        Abort(abortReason, error);
    }

    protected override string CreateRequestId()
    {
        return _context.StreamContext.ConnectionId;
    }

    protected override MessageBody CreateMessageBody()
    {
        if (_messageBody != null)
        {
            _messageBody.Reset();
        }
        else
        {
            _messageBody = new Http3MessageBody(this);
        }

        return _messageBody;
    }

    protected override bool TryParseRequest(ReadResult result, out bool endConnection)
    {
        endConnection = !TryValidatePseudoHeaders();

        // 431 if the headers are too large
        if (_totalParsedHeaderSize > ServerOptions.Limits.MaxRequestHeadersTotalSize)
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.HeadersExceedMaxTotalSize);
        }

        // 431 if we received too many headers
        if (RequestHeadersParsed > ServerOptions.Limits.MaxRequestHeaderCount)
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.TooManyHeaders);
        }

        // Suppress pseudo headers from the public headers collection.
        HttpRequestHeaders.ClearPseudoRequestHeaders();

        // Cookies should be merged into a single string separated by "; "
        // https://datatracker.ietf.org/doc/html/draft-ietf-quic-http-34#section-4.1.1.2
        HttpRequestHeaders.MergeCookies();

        return true;
    }

    private bool TryValidatePseudoHeaders()
    {
        _httpVersion = Http.HttpVersion.Http3;

        if (!TryValidateMethod())
        {
            return false;
        }

        if (!TryValidateAuthorityAndHost(out var hostText))
        {
            return false;
        }

        // CONNECT - :scheme and :path must be excluded=
        if (Method == HttpMethod.Connect && HttpRequestHeaders.HeaderProtocol.Count == 0)
        {
            if (!string.IsNullOrEmpty(HttpRequestHeaders.HeaderScheme) || !string.IsNullOrEmpty(HttpRequestHeaders.HeaderPath))
            {
                Abort(new ConnectionAbortedException(CoreStrings.Http3ErrorConnectMustNotSendSchemeOrPath), Http3ErrorCode.ProtocolError);
                return false;
            }

            RawTarget = hostText;

            return true;
        }

        // :scheme https://tools.ietf.org/html/rfc7540#section-8.1.2.3
        // ":scheme" is not restricted to "http" and "https" schemed URIs.  A
        // proxy or gateway can translate requests for non - HTTP schemes,
        // enabling the use of HTTP to interact with non - HTTP services.
        var headerScheme = HttpRequestHeaders.HeaderScheme.ToString();
        if (!ReferenceEquals(headerScheme, Scheme) &&
            !string.Equals(headerScheme, Scheme, StringComparison.OrdinalIgnoreCase))
        {
            if (!ServerOptions.AllowAlternateSchemes || !Uri.CheckSchemeName(headerScheme))
            {
                var str = CoreStrings.FormatHttp3StreamErrorSchemeMismatch(headerScheme, Scheme);
                Abort(new ConnectionAbortedException(str), Http3ErrorCode.ProtocolError);
                return false;
            }

            Scheme = headerScheme;
        }

        // :path (and query) - Required
        // Must start with / except may be * for OPTIONS
        var path = HttpRequestHeaders.HeaderPath.ToString();
        RawTarget = path;

        // OPTIONS - https://tools.ietf.org/html/rfc7540#section-8.1.2.3
        // This pseudo-header field MUST NOT be empty for "http" or "https"
        // URIs; "http" or "https" URIs that do not contain a path component
        // MUST include a value of '/'.  The exception to this rule is an
        // OPTIONS request for an "http" or "https" URI that does not include
        // a path component; these MUST include a ":path" pseudo-header field
        // with a value of '*'.
        if (Method == HttpMethod.Options && path.Length == 1 && path[0] == '*')
        {
            // * is stored in RawTarget only since HttpRequest expects Path to be empty or start with a /.
            Path = string.Empty;
            QueryString = string.Empty;
            return true;
        }

        // Approximate MaxRequestLineSize by totaling the required pseudo header field lengths.
        var requestLineLength = _methodText!.Length + Scheme!.Length + hostText.Length + path.Length;
        if (requestLineLength > ServerOptions.Limits.MaxRequestLineSize)
        {
            Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestLineTooLong), Http3ErrorCode.RequestRejected);
            return false;
        }

        var queryIndex = path.IndexOf('?');
        QueryString = queryIndex == -1 ? string.Empty : path.Substring(queryIndex);

        var pathSegment = queryIndex == -1 ? path.AsSpan() : path.AsSpan(0, queryIndex);

        return TryValidatePath(pathSegment);
    }

    private bool TryValidateMethod()
    {
        // :method
        _methodText = HttpRequestHeaders.HeaderMethod.ToString();
        Method = HttpUtilities.GetKnownMethod(_methodText);

        if (Method == HttpMethod.None)
        {
            Abort(new ConnectionAbortedException(CoreStrings.FormatHttp3ErrorMethodInvalid(_methodText)), Http3ErrorCode.ProtocolError);
            return false;
        }

        if (Method == HttpMethod.Custom)
        {
            if (HttpCharacters.IndexOfInvalidTokenChar(_methodText) >= 0)
            {
                Abort(new ConnectionAbortedException(CoreStrings.FormatHttp3ErrorMethodInvalid(_methodText)), Http3ErrorCode.ProtocolError);
                return false;
            }
        }

        return true;
    }

    private bool TryValidateAuthorityAndHost(out string hostText)
    {
        // :authority (optional)
        // Prefer this over Host

        var authority = HttpRequestHeaders.HeaderAuthority;
        var host = HttpRequestHeaders.HeaderHost;
        if (!StringValues.IsNullOrEmpty(authority))
        {
            // https://tools.ietf.org/html/rfc7540#section-8.1.2.3
            // Clients that generate HTTP/2 requests directly SHOULD use the ":authority"
            // pseudo - header field instead of the Host header field.
            // An intermediary that converts an HTTP/2 request to HTTP/1.1 MUST
            // create a Host header field if one is not present in a request by
            // copying the value of the ":authority" pseudo - header field.

            // We take this one step further, we don't want mismatched :authority
            // and Host headers, replace Host if :authority is defined. The application
            // will operate on the Host header.
            HttpRequestHeaders.HeaderHost = authority;
            host = authority;
        }

        // https://tools.ietf.org/html/rfc7230#section-5.4
        // A server MUST respond with a 400 (Bad Request) status code to any
        // HTTP/1.1 request message that lacks a Host header field and to any
        // request message that contains more than one Host header field or a
        // Host header field with an invalid field-value.
        hostText = host.ToString();
        if (host.Count > 1 || !HttpUtilities.IsHostHeaderValid(hostText))
        {
            // RST replaces 400
            Abort(new ConnectionAbortedException(CoreStrings.FormatBadRequest_InvalidHostHeader_Detail(hostText)), Http3ErrorCode.ProtocolError);
            return false;
        }

        return true;
    }

    [SkipLocalsInit]
    private bool TryValidatePath(ReadOnlySpan<char> pathSegment)
    {
        // Must start with a leading slash
        if (pathSegment.IsEmpty || pathSegment[0] != '/')
        {
            Abort(new ConnectionAbortedException(CoreStrings.FormatHttp3StreamErrorPathInvalid(RawTarget)), Http3ErrorCode.ProtocolError);
            return false;
        }

        var pathEncoded = pathSegment.Contains('%');

        // Compare with Http1Connection.OnOriginFormTarget

        // URIs are always encoded/escaped to ASCII https://tools.ietf.org/html/rfc3986#page-11
        // Multibyte Internationalized Resource Identifiers (IRIs) are first converted to utf8;
        // then encoded/escaped to ASCII  https://www.ietf.org/rfc/rfc3987.txt "Mapping of IRIs to URIs"

        try
        {
            const int MaxPathBufferStackAllocSize = 256;

            // The decoder operates only on raw bytes
            Span<byte> pathBuffer = pathSegment.Length <= MaxPathBufferStackAllocSize
                // A constant size plus slice generates better code
                // https://github.com/dotnet/aspnetcore/pull/19273#discussion_r383159929
                ? stackalloc byte[MaxPathBufferStackAllocSize].Slice(0, pathSegment.Length)
                // TODO - Consider pool here for less than 4096
                // https://github.com/dotnet/aspnetcore/pull/19273#discussion_r383604184
                : new byte[pathSegment.Length];

            for (var i = 0; i < pathSegment.Length; i++)
            {
                var ch = pathSegment[i];
                // The header parser should already be checking this
                Debug.Assert(32 < ch && ch < 127);
                pathBuffer[i] = (byte)ch;
            }

            Path = PathDecoder.DecodePath(pathBuffer, pathEncoded, RawTarget!, QueryString!.Length);

            return true;
        }
        catch (InvalidOperationException)
        {
            Abort(new ConnectionAbortedException(CoreStrings.FormatHttp3StreamErrorPathInvalid(RawTarget)), Http3ErrorCode.ProtocolError);
            return false;
        }
    }

    private Pipe CreateRequestBodyPipe(uint windowSize)
        => new Pipe(new PipeOptions
        (
            pool: _context.MemoryPool,
            readerScheduler: ServiceContext.Scheduler,
            writerScheduler: PipeScheduler.Inline,
            // Never pause within the window range. Flow control will prevent more data from being added.
            // See the assert in OnDataAsync.
            pauseWriterThreshold: windowSize + 1,
            resumeWriterThreshold: windowSize + 1,
            useSynchronizationContext: false,
            minimumSegmentSize: _context.MemoryPool.GetMinimumSegmentSize()
        ));

    private (StreamCompletionFlags OldState, StreamCompletionFlags NewState) ApplyCompletionFlag(StreamCompletionFlags completionState)
    {
        lock (_completionLock)
        {
            var oldCompletionState = _completionState;
            _completionState |= completionState;

            return (oldCompletionState, _completionState);
        }
    }

#pragma warning disable CA2252 // WebTransport is a preview feature
    public override async ValueTask<IWebTransportSession> AcceptAsync(CancellationToken token)
#pragma warning restore CA2252 // WebTransport is a preview feature
    {
        if (_isWebTransportSessionAccepted)
        {
            throw new InvalidOperationException(CoreStrings.AcceptCannotBeCalledMultipleTimes);
        }

        if (!_context.ServiceContext.ServerOptions.EnableWebTransportAndH3Datagrams)
        {
            throw new InvalidOperationException(CoreStrings.WebTransportIsDisabled);
        }

        if (!IsWebTransportRequest)
        {
            throw new InvalidOperationException(CoreStrings.FormatFailedToNegotiateCommonWebTransportVersion(WebTransportSession.CurrentSupportedVersion));
        }

        _isWebTransportSessionAccepted = true;

        // version negotiation
        var version = WebTransportSession.CurrentSupportedVersionSuffix;

        _context.WebTransportSession = _context.Connection!.OpenNewWebTransportSession(this);

        // send version negotiation resulting version
        ResponseHeaders[WebTransportSession.VersionHeaderPrefix] = version;
        await FlushAsync(token);

        return _context.WebTransportSession;
    }

    /// <summary>
    /// Used to kick off the request processing loop by derived classes.
    /// </summary>
    public abstract void Execute();

    public void Abort()
    {
        Abort(new(), Http3ErrorCode.RequestCancelled);
    }

    protected enum RequestHeaderParsingState
    {
        Ready,
        PseudoHeaderFields,
        Headers,
        Body,
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
