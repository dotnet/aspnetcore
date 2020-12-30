// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Http;
using System.Net.Http.QPack;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3
{
    internal abstract partial class Http3Stream : HttpProtocol, IHttpHeadersHandler, IThreadPoolWorkItem, ITimeoutHandler, IRequestProcessor
    {
        private static ReadOnlySpan<byte> AuthorityBytes => new byte[10] { (byte)':', (byte)'a', (byte)'u', (byte)'t', (byte)'h', (byte)'o', (byte)'r', (byte)'i', (byte)'t', (byte)'y' };
        private static ReadOnlySpan<byte> MethodBytes => new byte[7] { (byte)':', (byte)'m', (byte)'e', (byte)'t', (byte)'h', (byte)'o', (byte)'d' };
        private static ReadOnlySpan<byte> PathBytes => new byte[5] { (byte)':', (byte)'p', (byte)'a', (byte)'t', (byte)'h' };
        private static ReadOnlySpan<byte> SchemeBytes => new byte[7] { (byte)':', (byte)'s', (byte)'c', (byte)'h', (byte)'e', (byte)'m', (byte)'e' };
        private static ReadOnlySpan<byte> StatusBytes => new byte[7] { (byte)':', (byte)'s', (byte)'t', (byte)'a', (byte)'t', (byte)'u', (byte)'s' };
        private static ReadOnlySpan<byte> ConnectionBytes => new byte[10] { (byte)'c', (byte)'o', (byte)'n', (byte)'n', (byte)'e', (byte)'c', (byte)'t', (byte)'i', (byte)'o', (byte)'n' };
        private static ReadOnlySpan<byte> TeBytes => new byte[2] { (byte)'t', (byte)'e' };
        private static ReadOnlySpan<byte> TrailersBytes => new byte[8] { (byte)'t', (byte)'r', (byte)'a', (byte)'i', (byte)'l', (byte)'e', (byte)'r', (byte)'s' };
        private static ReadOnlySpan<byte> ConnectBytes => new byte[7] { (byte)'C', (byte)'O', (byte)'N', (byte)'N', (byte)'E', (byte)'C', (byte)'T' };

        private readonly Http3FrameWriter _frameWriter;
        private readonly Http3OutputProducer _http3Output;
        private int _isClosed;
        private int _gracefulCloseInitiator;
        private readonly Http3StreamContext _context;
        private readonly IProtocolErrorCodeFeature _errorCodeFeature;
        private readonly IStreamIdFeature _streamIdFeature;
        private readonly Http3RawFrame _incomingFrame = new Http3RawFrame();
        protected RequestHeaderParsingState _requestHeaderParsingState;
        private PseudoHeaderFields _parsedPseudoHeaderFields;
        private bool _isMethodConnect;

        private readonly Http3Connection _http3Connection;
        private bool _receivedHeaders;
        private TaskCompletionSource? _appCompleted;

        public Pipe RequestBodyPipe { get; }

        public Http3Stream(Http3Connection http3Connection, Http3StreamContext context)
        {
            Initialize(context);

            InputRemaining = null;

            // First, determine how we know if an Http3stream is unidirectional or bidirectional
            var httpLimits = context.ServiceContext.ServerOptions.Limits;
            var http3Limits = httpLimits.Http3;
            _http3Connection = http3Connection;
            _context = context;

            _errorCodeFeature = _context.ConnectionFeatures.Get<IProtocolErrorCodeFeature>()!;
            _streamIdFeature = _context.ConnectionFeatures.Get<IStreamIdFeature>()!;

            _frameWriter = new Http3FrameWriter(
                context.Transport.Output,
                context.StreamContext,
                context.TimeoutControl,
                httpLimits.MinResponseDataRate,
                context.ConnectionId,
                context.MemoryPool,
                context.ServiceContext.Log);

            // ResponseHeaders aren't set, kind of ugly that we need to reset.
            Reset();

            _http3Output = new Http3OutputProducer(
                _frameWriter,
                context.MemoryPool,
                this,
                context.ServiceContext.Log);
            RequestBodyPipe = CreateRequestBodyPipe(64 * 1024); // windowSize?
            Output = _http3Output;
            QPackDecoder = new QPackDecoder(_context.ServiceContext.ServerOptions.Limits.Http3.MaxRequestHeaderFieldSize);
        }

        public long? InputRemaining { get; internal set; }

        public QPackDecoder QPackDecoder { get; }

        public PipeReader Input => _context.Transport.Input;

        public ISystemClock SystemClock => _context.ServiceContext.SystemClock;
        public KestrelServerLimits Limits => _context.ServiceContext.ServerOptions.Limits;

        public void Abort(ConnectionAbortedException ex)
        {
            AbortCore(ex, Http3ErrorCode.InternalError);
        }

        public void Abort(ConnectionAbortedException ex, Http3ErrorCode errorCode)
        {
            AbortCore(ex, errorCode);
        }

        private void AbortCore(ConnectionAbortedException abortReason, Http3ErrorCode errorCode)
        {
            _errorCodeFeature.Error = (long)errorCode;
            _frameWriter.Abort(abortReason);

            // Call _http3Output.Stop() prior to poisoning the request body stream or pipe to
            // ensure that an app that completes early due to the abort doesn't result in header frames being sent.
            _http3Output.Stop();

            CancelRequestAbortedToken();

            // Unblock the request body.
            PoisonBody(abortReason);
            RequestBodyPipe.Writer.Complete(abortReason);
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
            var knownHeader = H3StaticTable.GetHeaderFieldAt(index);
            OnHeader(knownHeader.Name, knownHeader.Value);
        }

        public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
            var knownHeader = H3StaticTable.GetHeaderFieldAt(index);
            OnHeader(knownHeader.Name, value);
        }

        public override void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            // TODO MaxRequestHeadersTotalSize?
            ValidateHeader(name, value);
            try
            {
                if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
                {
                    OnTrailer(name, value);
                }
                else
                {
                    // Throws BadRequest for header count limit breaches.
                    // Throws InvalidOperation for bad encoding.
                    base.OnHeader(name, value);
                }
            }
            catch (Microsoft.AspNetCore.Http.BadHttpRequestException bre)
            {
                throw new Http3StreamErrorException(bre.Message, Http3ErrorCode.ProtocolError);
            }
            catch (InvalidOperationException)
            {
                throw new Http3StreamErrorException(CoreStrings.BadRequest_MalformedRequestInvalidHeaders, Http3ErrorCode.ProtocolError);
            }
        }

        private void ValidateHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
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
            if (IsPseudoHeaderField(name, out var headerField))
            {
                if (_requestHeaderParsingState == RequestHeaderParsingState.Headers)
                {
                    // All pseudo-header fields MUST appear in the header block before regular header fields.
                    // Any request or response that contains a pseudo-header field that appears in a header
                    // block after a regular header field MUST be treated as malformed (Section 8.1.2.6).
                    throw new Http3StreamErrorException(CoreStrings.Http2ErrorPseudoHeaderFieldAfterRegularHeaders, Http3ErrorCode.ProtocolError);
                }

                if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
                {
                    // Pseudo-header fields MUST NOT appear in trailers.
                    throw new Http3StreamErrorException(CoreStrings.Http2ErrorTrailersContainPseudoHeaderField, Http3ErrorCode.ProtocolError);
                }

                _requestHeaderParsingState = RequestHeaderParsingState.PseudoHeaderFields;

                if (headerField == PseudoHeaderFields.Unknown)
                {
                    // Endpoints MUST treat a request or response that contains undefined or invalid pseudo-header
                    // fields as malformed (Section 8.1.2.6).
                    throw new Http3StreamErrorException(CoreStrings.Http2ErrorUnknownPseudoHeaderField, Http3ErrorCode.ProtocolError);
                }

                if (headerField == PseudoHeaderFields.Status)
                {
                    // Pseudo-header fields defined for requests MUST NOT appear in responses; pseudo-header fields
                    // defined for responses MUST NOT appear in requests.
                    throw new Http3StreamErrorException(CoreStrings.Http2ErrorResponsePseudoHeaderField, Http3ErrorCode.ProtocolError);
                }

                if ((_parsedPseudoHeaderFields & headerField) == headerField)
                {
                    // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2.3
                    // All HTTP/2 requests MUST include exactly one valid value for the :method, :scheme, and :path pseudo-header fields
                    throw new Http3StreamErrorException(CoreStrings.Http2ErrorDuplicatePseudoHeaderField, Http3ErrorCode.ProtocolError);
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

            if (IsConnectionSpecificHeaderField(name, value))
            {
                throw new Http3StreamErrorException(CoreStrings.Http2ErrorConnectionSpecificHeaderField, Http3ErrorCode.ProtocolError);
            }

            // http://httpwg.org/specs/rfc7540.html#rfc.section.8.1.2
            // A request or response containing uppercase header field names MUST be treated as malformed (Section 8.1.2.6).
            for (var i = 0; i < name.Length; i++)
            {
                if (name[i] >= 65 && name[i] <= 90)
                {
                    if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
                    {
                        throw new Http3StreamErrorException(CoreStrings.Http2ErrorTrailerNameUppercase, Http3ErrorCode.ProtocolError);
                    }
                    else
                    {
                        throw new Http3StreamErrorException(CoreStrings.Http2ErrorHeaderNameUppercase, Http3ErrorCode.ProtocolError);
                    }
                }
            }
        }

        private bool IsPseudoHeaderField(ReadOnlySpan<byte> name, out PseudoHeaderFields headerField)
        {
            headerField = PseudoHeaderFields.None;

            if (name.IsEmpty || name[0] != (byte)':')
            {
                return false;
            }

            if (name.SequenceEqual(PathBytes))
            {
                headerField = PseudoHeaderFields.Path;
            }
            else if (name.SequenceEqual(MethodBytes))
            {
                headerField = PseudoHeaderFields.Method;
            }
            else if (name.SequenceEqual(SchemeBytes))
            {
                headerField = PseudoHeaderFields.Scheme;
            }
            else if (name.SequenceEqual(StatusBytes))
            {
                headerField = PseudoHeaderFields.Status;
            }
            else if (name.SequenceEqual(AuthorityBytes))
            {
                headerField = PseudoHeaderFields.Authority;
            }
            else
            {
                headerField = PseudoHeaderFields.Unknown;
            }

            return true;
        }

        private static bool IsConnectionSpecificHeaderField(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
            return name.SequenceEqual(ConnectionBytes) || (name.SequenceEqual(TeBytes) && !value.SequenceEqual(TrailersBytes));
        }

        public void HandleReadDataRateTimeout()
        {
            Debug.Assert(Limits.MinRequestBodyDataRate != null);

            Log.RequestBodyMinimumDataRateNotSatisfied(ConnectionId, null, Limits.MinRequestBodyDataRate.BytesPerSecond);
            Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestBodyTimeout), Http3ErrorCode.RequestRejected);
        }

        public void HandleRequestHeadersTimeout()
        {
            Log.ConnectionBadRequest(ConnectionId, KestrelBadHttpRequestException.GetException(RequestRejectionReason.RequestHeadersTimeout));
            Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestHeadersTimeout), Http3ErrorCode.RequestRejected);
        }

        public void OnInputOrOutputCompleted()
        {
            TryClose();
            Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient), Http3ErrorCode.NoError);
        }

        protected override void OnRequestProcessingEnded()
        {
            Debug.Assert(_appCompleted != null);
            _appCompleted.SetResult();
        }

        private bool TryClose()
        {
            if (Interlocked.Exchange(ref _isClosed, 1) == 0)
            {
                return true;
            }

            // TODO make this actually close the Http3Stream by telling quic to close the stream.
            return false;
        }

        public async Task ProcessRequestAsync<TContext>(IHttpApplication<TContext> application) where TContext : notnull
        {
            Exception? error = null;

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
                            while (Http3FrameReader.TryReadFrame(ref readableBuffer, _incomingFrame, 16 * 1024, out var framePayload))
                            {
                                consumed = examined = framePayload.End;
                                await ProcessHttp3Stream(application, framePayload);
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
            // catch ConnectionResetException here?
            catch (Http3StreamErrorException ex)
            {
                error = ex;
                Abort(new ConnectionAbortedException(ex.Message, ex), ex.ErrorCode);
            }
            catch (Exception ex)
            {
                error = ex;
                Log.LogWarning(0, ex, "Stream threw an unexpected exception.");
            }
            finally
            {
                var streamError = error as ConnectionAbortedException
                    ?? new ConnectionAbortedException("The stream has completed.", error!);

                await Input.CompleteAsync();

                // Make sure application func is completed before completing writer.
                if (_appCompleted != null)
                {
                    await _appCompleted.Task;
                }

                try
                {
                    await _frameWriter.CompleteAsync();
                }
                catch
                {
                    Abort(streamError, Http3ErrorCode.ProtocolError);
                    throw;
                }
                finally
                {
                    _http3Connection.RemoveStream(_streamIdFeature.StreamId);
                }
            }
        }

        private ValueTask OnEndStreamReceived()
        {
            if (InputRemaining.HasValue)
            {
                // https://tools.ietf.org/html/rfc7540#section-8.1.2.6
                if (InputRemaining.Value != 0)
                {
                    throw new Http3StreamErrorException(CoreStrings.Http3StreamErrorLessDataThanLength, Http3ErrorCode.ProtocolError);
                }
            }

            OnTrailersComplete();
            return RequestBodyPipe.Writer.CompleteAsync();
        }

        private Task ProcessHttp3Stream<TContext>(IHttpApplication<TContext> application, in ReadOnlySequence<byte> payload) where TContext : notnull
        {
            switch (_incomingFrame.Type)
            {
                case Http3FrameType.Data:
                    return ProcessDataFrameAsync(payload);
                case Http3FrameType.Headers:
                    return ProcessHeadersFrameAsync(application, payload);
                // need to be on control stream
                case Http3FrameType.DuplicatePush:
                case Http3FrameType.PushPromise:
                case Http3FrameType.Settings:
                case Http3FrameType.GoAway:
                case Http3FrameType.CancelPush:
                case Http3FrameType.MaxPushId:
                    throw new Http3ConnectionException("HTTP_FRAME_UNEXPECTED");
                default:
                    return ProcessUnknownFrameAsync();
            }
        }

        private Task ProcessUnknownFrameAsync()
        {
            // Unknown frames must be explicitly ignored.
            return Task.CompletedTask;
        }

        private Task ProcessHeadersFrameAsync<TContext>(IHttpApplication<TContext> application, ReadOnlySequence<byte> payload) where TContext : notnull
        {
            QPackDecoder.Decode(payload, handler: this);

            // start off a request once qpack has decoded
            // Make sure to await this task.
            if (_receivedHeaders)
            {
                // trailers
                // TODO figure out if there is anything else to do here.
                return Task.CompletedTask;
            }

            _receivedHeaders = true;
            InputRemaining = HttpRequestHeaders.ContentLength;

            _appCompleted = new TaskCompletionSource();

            ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);

            return Task.CompletedTask;
        }

        private Task ProcessDataFrameAsync(in ReadOnlySequence<byte> payload)
        {
            if (InputRemaining.HasValue)
            {
                // https://tools.ietf.org/html/rfc7540#section-8.1.2.6
                if (payload.Length > InputRemaining.Value)
                {
                    throw new Http3StreamErrorException(CoreStrings.Http3StreamErrorMoreDataThanLength, Http3ErrorCode.ProtocolError);
                }

                InputRemaining -= payload.Length;
            }

            foreach (var segment in payload)
            {
                RequestBodyPipe.Writer.Write(segment.Span);
            }

            // TODO this can be better.
            return RequestBodyPipe.Writer.FlushAsync().AsTask();
        }

        public void StopProcessingNextRequest()
            => StopProcessingNextRequest(serverInitiated: true);

        public void StopProcessingNextRequest(bool serverInitiated)
        {
            var initiator = serverInitiated ? GracefulCloseInitiator.Server : GracefulCloseInitiator.Client;

            if (Interlocked.CompareExchange(ref _gracefulCloseInitiator, initiator, GracefulCloseInitiator.None) == GracefulCloseInitiator.None)
            {
                Input.CancelPendingRead();
            }
        }

        public void Tick(DateTimeOffset now)
        {
        }

        protected override void OnReset()
        {
            ResetHttp3Features();
        }

        protected override void ApplicationAbort() => ApplicationAbort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication), Http3ErrorCode.InternalError);

        private void ApplicationAbort(ConnectionAbortedException abortReason, Http3ErrorCode error)
        {
            Log.Http3StreamResetAbort(TraceIdentifier, error, abortReason);

            AbortCore(abortReason, error);
        }

        protected override string CreateRequestId()
        {
            // TODO include stream id.
            return ConnectionId;
        }

        protected override MessageBody CreateMessageBody()
            => Http3MessageBody.For(this);


        protected override bool TryParseRequest(ReadResult result, out bool endConnection)
        {
            endConnection = !TryValidatePseudoHeaders();
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

            // CONNECT - :scheme and :path must be excluded
            if (Method == Http.HttpMethod.Connect)
            {
                if (!string.IsNullOrEmpty(RequestHeaders[HeaderNames.Scheme]) || !string.IsNullOrEmpty(RequestHeaders[HeaderNames.Path]))
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

            // - That said, we shouldn't allow arbitrary values or use them to populate Request.Scheme, right?
            // - For now we'll restrict it to http/s and require it match the transport.
            // - We'll need to find some concrete scenarios to warrant unblocking this.
            if (!string.Equals(RequestHeaders[HeaderNames.Scheme], Scheme, StringComparison.OrdinalIgnoreCase))
            {
                var str = CoreStrings.FormatHttp3StreamErrorSchemeMismatch(RequestHeaders[HeaderNames.Scheme], Scheme);
                Abort(new ConnectionAbortedException(str), Http3ErrorCode.ProtocolError);
                return false;
            }

            // :path (and query) - Required
            // Must start with / except may be * for OPTIONS
            var path = RequestHeaders[HeaderNames.Path].ToString();
            RawTarget = path;

            // OPTIONS - https://tools.ietf.org/html/rfc7540#section-8.1.2.3
            // This pseudo-header field MUST NOT be empty for "http" or "https"
            // URIs; "http" or "https" URIs that do not contain a path component
            // MUST include a value of '/'.  The exception to this rule is an
            // OPTIONS request for an "http" or "https" URI that does not include
            // a path component; these MUST include a ":path" pseudo-header field
            // with a value of '*'.
            if (Method == Http.HttpMethod.Options && path.Length == 1 && path[0] == '*')
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
                Abort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestLineTooLong), Http3ErrorCode.ProtocolError);
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
            _methodText = RequestHeaders[HeaderNames.Method].ToString();
            Method = HttpUtilities.GetKnownMethod(_methodText);

            if (Method == Http.HttpMethod.None)
            {
                Abort(new ConnectionAbortedException(CoreStrings.FormatHttp3ErrorMethodInvalid(_methodText)), Http3ErrorCode.ProtocolError);
                return false;
            }

            if (Method == Http.HttpMethod.Custom)
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

            var authority = RequestHeaders[HeaderNames.Authority];
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

        private bool TryValidatePath(ReadOnlySpan<char> pathSegment)
        {
            // Must start with a leading slash
            if (pathSegment.Length == 0 || pathSegment[0] != '/')
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

                Path = PathNormalizer.DecodePath(pathBuffer, pathEncoded, RawTarget!, QueryString!.Length);

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

        /// <summary>
        /// Used to kick off the request processing loop by derived classes.
        /// </summary>
        public abstract void Execute();

        public void OnTimeout(TimeoutReason reason)
        {
            throw new NotImplementedException();
        }

        protected enum RequestHeaderParsingState
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
            Unknown = 0x40000000
        }

        private static class GracefulCloseInitiator
        {
            public const int None = 0;
            public const int Server = 1;
            public const int Client = 2;
        }
    }
}
