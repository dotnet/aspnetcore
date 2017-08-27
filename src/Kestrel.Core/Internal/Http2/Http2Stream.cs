// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web.Utf8;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

// ReSharper disable AccessToModifiedClosure

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public abstract partial class Http2Stream : IFrameControl
    {
        private const byte ByteAsterisk = (byte)'*';
        private const byte ByteForwardSlash = (byte)'/';
        private const byte BytePercentage = (byte)'%';

        private static readonly byte[] _bytesServer = Encoding.ASCII.GetBytes("\r\nServer: " + Constants.ServerName);

        private const string EmptyPath = "/";
        private const string Asterisk = "*";

        private readonly object _onStartingSync = new Object();
        private readonly object _onCompletedSync = new Object();

        private Http2StreamContext _context;
        private Http2Streams _streams;

        protected Stack<KeyValuePair<Func<object, Task>, object>> _onStarting;
        protected Stack<KeyValuePair<Func<object, Task>, object>> _onCompleted;

        protected int _requestAborted;
        private CancellationTokenSource _abortedCts;
        private CancellationToken? _manuallySetRequestAbortToken;

        protected RequestProcessingStatus _requestProcessingStatus;
        private bool _canHaveBody;
        protected Exception _applicationException;
        private BadHttpRequestException _requestRejectedException;

        private string _requestId;

        protected long _responseBytesWritten;

        private HttpRequestTarget _requestTargetForm = HttpRequestTarget.Unknown;
        private Uri _absoluteRequestTarget;
        private string _scheme = null;

        public Http2Stream(Http2StreamContext context)
        {
            _context = context;
            HttpStreamControl = this;
            ServerOptions = context.ServiceContext.ServerOptions;
            RequestBodyPipe = CreateRequestBodyPipe();
        }

        public IFrameControl HttpStreamControl { get; set; }

        public Http2MessageBody MessageBody { get; protected set; }
        public IPipe RequestBodyPipe { get; }

        protected string ConnectionId => _context.ConnectionId;
        public int StreamId => _context.StreamId;
        public ServiceContext ServiceContext => _context.ServiceContext;

        // Hold direct reference to ServerOptions since this is used very often in the request processing path
        private KestrelServerOptions ServerOptions { get; }

        public IFeatureCollection ConnectionFeatures { get; set; }
        protected IHttp2StreamLifetimeHandler StreamLifetimeHandler => _context.StreamLifetimeHandler;
        public IHttp2FrameWriter Output => _context.FrameWriter;

        protected IKestrelTrace Log => ServiceContext.Log;
        private DateHeaderValueManager DateHeaderValueManager => ServiceContext.DateHeaderValueManager;

        private IPEndPoint LocalEndPoint => _context.LocalEndPoint;
        private IPEndPoint RemoteEndPoint => _context.RemoteEndPoint;

        public string ConnectionIdFeature { get; set; }
        public bool HasStartedConsumingRequestBody { get; set; }
        public long? MaxRequestBodySize { get; set; }
        public bool AllowSynchronousIO { get; set; }

        public bool ExpectBody { get; set; }

        /// <summary>
        /// The request id. <seealso cref="HttpContext.TraceIdentifier"/>
        /// </summary>
        public string TraceIdentifier
        {
            set => _requestId = value;
            get
            {
                // don't generate an ID until it is requested
                if (_requestId == null)
                {
                    _requestId = StringUtilities.ConcatAsHexSuffix(ConnectionId, ':', (uint)StreamId);
                }
                return _requestId;
            }
        }

        public IPAddress RemoteIpAddress { get; set; }
        public int RemotePort { get; set; }
        public IPAddress LocalIpAddress { get; set; }
        public int LocalPort { get; set; }
        public string Scheme { get; set; }
        public string Method { get; set; }
        public string PathBase { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string RawTarget { get; set; }

        public string HttpVersion => "HTTP/2";

        public IHeaderDictionary RequestHeaders { get; set; }
        public Stream RequestBody { get; set; }

        private int _statusCode;
        public int StatusCode
        {
            get => _statusCode;
            set
            {
                if (HasResponseStarted)
                {
                    ThrowResponseAlreadyStartedException(nameof(StatusCode));
                }

                _statusCode = value;
            }
        }

        private string _reasonPhrase;

        public string ReasonPhrase
        {
            get => _reasonPhrase;

            set
            {
                if (HasResponseStarted)
                {
                    ThrowResponseAlreadyStartedException(nameof(ReasonPhrase));
                }

                _reasonPhrase = value;
            }
        }

        public IHeaderDictionary ResponseHeaders { get; set; }
        public Stream ResponseBody { get; set; }

        public CancellationToken RequestAborted
        {
            get
            {
                // If a request abort token was previously explicitly set, return it.
                if (_manuallySetRequestAbortToken.HasValue)
                {
                    return _manuallySetRequestAbortToken.Value;
                }
                // Otherwise, get the abort CTS.  If we have one, which would mean that someone previously
                // asked for the RequestAborted token, simply return its token.  If we don't,
                // check to see whether we've already aborted, in which case just return an
                // already canceled token.  Finally, force a source into existence if we still
                // don't have one, and return its token.
                var cts = _abortedCts;
                return
                    cts != null ? cts.Token :
                    (Volatile.Read(ref _requestAborted) == 1) ? new CancellationToken(true) :
                    RequestAbortedSource.Token;
            }
            set
            {
                // Set an abort token, overriding one we create internally.  This setter and associated
                // field exist purely to support IHttpRequestLifetimeFeature.set_RequestAborted.
                _manuallySetRequestAbortToken = value;
            }
        }

        private CancellationTokenSource RequestAbortedSource
        {
            get
            {
                // Get the abort token, lazily-initializing it if necessary.
                // Make sure it's canceled if an abort request already came in.

                // EnsureInitialized can return null since _abortedCts is reset to null
                // after it's already been initialized to a non-null value.
                // If EnsureInitialized does return null, this property was accessed between
                // requests so it's safe to return an ephemeral CancellationTokenSource.
                var cts = LazyInitializer.EnsureInitialized(ref _abortedCts, () => new CancellationTokenSource())
                            ?? new CancellationTokenSource();

                if (Volatile.Read(ref _requestAborted) == 1)
                {
                    cts.Cancel();
                }
                return cts;
            }
        }

        public bool HasResponseStarted => _requestProcessingStatus == RequestProcessingStatus.ResponseStarted;

        protected FrameRequestHeaders FrameRequestHeaders { get; } = new FrameRequestHeaders();

        protected FrameResponseHeaders FrameResponseHeaders { get; } = new FrameResponseHeaders();

        public void InitializeStreams(Http2MessageBody messageBody)
        {
            if (_streams == null)
            {
                _streams = new Http2Streams(bodyControl: this, httpStreamControl: this);
            }

            (RequestBody, ResponseBody) = _streams.Start(messageBody);
        }

        public void PauseStreams() => _streams.Pause();

        public void StopStreams() => _streams.Stop();

        public void Reset()
        {
            _onStarting = null;
            _onCompleted = null;

            _requestProcessingStatus = RequestProcessingStatus.RequestPending;
            _applicationException = null;

            ResetFeatureCollection();

            HasStartedConsumingRequestBody = false;
            MaxRequestBodySize = ServerOptions.Limits.MaxRequestBodySize;
            AllowSynchronousIO = ServerOptions.AllowSynchronousIO;
            TraceIdentifier = null;
            Method = null;
            PathBase = null;
            Path = null;
            RawTarget = null;
            _requestTargetForm = HttpRequestTarget.Unknown;
            _absoluteRequestTarget = null;
            QueryString = null;
            _statusCode = StatusCodes.Status200OK;
            _reasonPhrase = null;

            RemoteIpAddress = RemoteEndPoint?.Address;
            RemotePort = RemoteEndPoint?.Port ?? 0;

            LocalIpAddress = LocalEndPoint?.Address;
            LocalPort = LocalEndPoint?.Port ?? 0;
            ConnectionIdFeature = ConnectionId;

            FrameRequestHeaders.Reset();
            FrameResponseHeaders.Reset();
            RequestHeaders = FrameRequestHeaders;
            ResponseHeaders = FrameResponseHeaders;

            if (_scheme == null)
            {
                var tlsFeature = ConnectionFeatures?[typeof(ITlsConnectionFeature)];
                _scheme = tlsFeature != null ? "https" : "http";
            }

            Scheme = _scheme;

            _manuallySetRequestAbortToken = null;
            _abortedCts = null;

            _responseBytesWritten = 0;
        }

        private void CancelRequestAbortedToken()
        {
            try
            {
                RequestAbortedSource.Cancel();
                _abortedCts = null;
            }
            catch (Exception ex)
            {
                Log.ApplicationError(ConnectionId, TraceIdentifier, ex);
            }
        }

        public void Abort(Exception error)
        {
            if (Interlocked.Exchange(ref _requestAborted, 1) == 0)
            {
                _streams?.Abort(error);

                // Potentially calling user code. CancelRequestAbortedToken logs any exceptions.
                ServiceContext.ThreadPool.UnsafeRun(state => ((Http2Stream)state).CancelRequestAbortedToken(), this);
            }
        }

        public abstract Task ProcessRequestAsync();

        public void OnStarting(Func<object, Task> callback, object state)
        {
            lock (_onStartingSync)
            {
                if (HasResponseStarted)
                {
                    ThrowResponseAlreadyStartedException(nameof(OnStarting));
                }

                if (_onStarting == null)
                {
                    _onStarting = new Stack<KeyValuePair<Func<object, Task>, object>>();
                }
                _onStarting.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
            }
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            lock (_onCompletedSync)
            {
                if (_onCompleted == null)
                {
                    _onCompleted = new Stack<KeyValuePair<Func<object, Task>, object>>();
                }
                _onCompleted.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
            }
        }

        protected async Task FireOnStarting()
        {
            Stack<KeyValuePair<Func<object, Task>, object>> onStarting = null;
            lock (_onStartingSync)
            {
                onStarting = _onStarting;
                _onStarting = null;
            }
            if (onStarting != null)
            {
                try
                {
                    foreach (var entry in onStarting)
                    {
                        await entry.Key.Invoke(entry.Value);
                    }
                }
                catch (Exception ex)
                {
                    ReportApplicationError(ex);
                }
            }
        }

        protected async Task FireOnCompleted()
        {
            Stack<KeyValuePair<Func<object, Task>, object>> onCompleted = null;
            lock (_onCompletedSync)
            {
                onCompleted = _onCompleted;
                _onCompleted = null;
            }
            if (onCompleted != null)
            {
                foreach (var entry in onCompleted)
                {
                    try
                    {
                        await entry.Key.Invoke(entry.Value);
                    }
                    catch (Exception ex)
                    {
                        ReportApplicationError(ex);
                    }
                }
            }
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await InitializeResponse(0);
            await Output.FlushAsync(cancellationToken);
        }

        public Task WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!HasResponseStarted)
            {
                return WriteAsyncAwaited(data, cancellationToken);
            }

            VerifyAndUpdateWrite(data.Count);

            if (_canHaveBody)
            {
                CheckLastWrite();
                return Output.WriteDataAsync(StreamId, data, cancellationToken: cancellationToken);
            }
            else
            {
                HandleNonBodyResponseWrite();
                return Task.CompletedTask;
            }
        }

        public async Task WriteAsyncAwaited(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            await InitializeResponseAwaited(data.Count);

            // WriteAsyncAwaited is only called for the first write to the body.
            // Ensure headers are flushed if Write(Chunked)Async isn't called.
            if (_canHaveBody)
            {
                CheckLastWrite();
                await Output.WriteDataAsync(StreamId, data, cancellationToken: cancellationToken);
            }
            else
            {
                HandleNonBodyResponseWrite();
                await FlushAsync(cancellationToken);
            }
        }

        private void VerifyAndUpdateWrite(int count)
        {
            var responseHeaders = FrameResponseHeaders;

            if (responseHeaders != null &&
                !responseHeaders.HasTransferEncoding &&
                responseHeaders.ContentLength.HasValue &&
                _responseBytesWritten + count > responseHeaders.ContentLength.Value)
            {
                throw new InvalidOperationException(
                    CoreStrings.FormatTooManyBytesWritten(_responseBytesWritten + count, responseHeaders.ContentLength.Value));
            }

            _responseBytesWritten += count;
        }

        private void CheckLastWrite()
        {
            var responseHeaders = FrameResponseHeaders;

            // Prevent firing request aborted token if this is the last write, to avoid
            // aborting the request if the app is still running when the client receives
            // the final bytes of the response and gracefully closes the connection.
            //
            // Called after VerifyAndUpdateWrite(), so _responseBytesWritten has already been updated.
            if (responseHeaders != null &&
                !responseHeaders.HasTransferEncoding &&
                responseHeaders.ContentLength.HasValue &&
                _responseBytesWritten == responseHeaders.ContentLength.Value)
            {
                _abortedCts = null;
            }
        }

        protected void VerifyResponseContentLength()
        {
            var responseHeaders = FrameResponseHeaders;

            if (!HttpMethods.IsHead(Method) &&
                !responseHeaders.HasTransferEncoding &&
                responseHeaders.ContentLength.HasValue &&
                _responseBytesWritten < responseHeaders.ContentLength.Value)
            {
                // We need to close the connection if any bytes were written since the client
                // cannot be certain of how many bytes it will receive.
                if (_responseBytesWritten > 0)
                {
                    // TODO: HTTP/2
                }

                ReportApplicationError(new InvalidOperationException(
                    CoreStrings.FormatTooFewBytesWritten(_responseBytesWritten, responseHeaders.ContentLength.Value)));
            }
        }

        private static ArraySegment<byte> CreateAsciiByteArraySegment(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            return new ArraySegment<byte>(bytes);
        }

        public void ProduceContinue()
        {
            if (HasResponseStarted)
            {
                return;
            }

            if (RequestHeaders.TryGetValue("Expect", out var expect) &&
                (expect.FirstOrDefault() ?? "").Equals("100-continue", StringComparison.OrdinalIgnoreCase))
            {
                Output.Write100ContinueAsync(StreamId).GetAwaiter().GetResult();
            }
        }

        public Task InitializeResponse(int firstWriteByteCount)
        {
            if (HasResponseStarted)
            {
                return Task.CompletedTask;
            }

            if (_onStarting != null)
            {
                return InitializeResponseAwaited(firstWriteByteCount);
            }

            if (_applicationException != null)
            {
                ThrowResponseAbortedException();
            }

            VerifyAndUpdateWrite(firstWriteByteCount);

            return ProduceStart(appCompleted: false);
        }

        private async Task InitializeResponseAwaited(int firstWriteByteCount)
        {
            await FireOnStarting();

            if (_applicationException != null)
            {
                ThrowResponseAbortedException();
            }

            VerifyAndUpdateWrite(firstWriteByteCount);

            await ProduceStart(appCompleted: false);
        }

        private Task ProduceStart(bool appCompleted)
        {
            if (HasResponseStarted)
            {
                return Task.CompletedTask;
            }

            _requestProcessingStatus = RequestProcessingStatus.ResponseStarted;

            return CreateResponseHeader(appCompleted);
        }

        protected Task TryProduceInvalidRequestResponse()
        {
            if (_requestRejectedException != null)
            {
                return ProduceEnd();
            }

            return Task.CompletedTask;
        }

        protected Task ProduceEnd()
        {
            if (_requestRejectedException != null || _applicationException != null)
            {
                if (HasResponseStarted)
                {
                    // We can no longer change the response, so we simply close the connection.
                    return Task.CompletedTask;
                }

                // If the request was rejected, the error state has already been set by SetBadRequestState and
                // that should take precedence.
                if (_requestRejectedException != null)
                {
                    SetErrorResponseException(_requestRejectedException);
                }
                else
                {
                    // 500 Internal Server Error
                    SetErrorResponseHeaders(statusCode: StatusCodes.Status500InternalServerError);
                }
            }

            if (!HasResponseStarted)
            {
                return ProduceEndAwaited();
            }

            return WriteSuffix();
        }

        private async Task ProduceEndAwaited()
        {
            await ProduceStart(appCompleted: true);

            // Force flush
            await Output.FlushAsync(default(CancellationToken));

            await WriteSuffix();
        }

        private Task WriteSuffix()
        {
            if (HttpMethods.IsHead(Method) && _responseBytesWritten > 0)
            {
                Log.ConnectionHeadResponseBodyWrite(ConnectionId, _responseBytesWritten);
            }

            return Output.WriteDataAsync(StreamId, Span<byte>.Empty, endStream: true, cancellationToken: default(CancellationToken));
        }

        private Task CreateResponseHeader(bool appCompleted)
        {
            var responseHeaders = FrameResponseHeaders;
            var hasConnection = responseHeaders.HasConnection;
            var connectionOptions = FrameHeaders.ParseConnection(responseHeaders.HeaderConnection);
            var hasTransferEncoding = responseHeaders.HasTransferEncoding;
            var transferCoding = FrameHeaders.GetFinalTransferCoding(responseHeaders.HeaderTransferEncoding);

            // https://tools.ietf.org/html/rfc7230#section-3.3.1
            // If any transfer coding other than
            // chunked is applied to a response payload body, the sender MUST either
            // apply chunked as the final transfer coding or terminate the message
            // by closing the connection.
            if (hasTransferEncoding && transferCoding == TransferCoding.Chunked)
            {
                // TODO: this is an error in HTTP/2
            }

            // Set whether response can have body
            _canHaveBody = StatusCanHaveBody(StatusCode) && Method != "HEAD";

            // Don't set the Content-Length or Transfer-Encoding headers
            // automatically for HEAD requests or 204, 205, 304 responses.
            if (_canHaveBody)
            {
                if (appCompleted)
                {
                    // Since the app has completed and we are only now generating
                    // the headers we can safely set the Content-Length to 0.
                    responseHeaders.ContentLength = 0;
                }
            }
            else if (hasTransferEncoding)
            {
                RejectNonBodyTransferEncodingResponse(appCompleted);
            }

            responseHeaders.SetReadOnly();

            if (ServerOptions.AddServerHeader && !responseHeaders.HasServer)
            {
                responseHeaders.SetRawServer(Constants.ServerName, _bytesServer);
            }

            if (!responseHeaders.HasDate)
            {
                var dateHeaderValues = DateHeaderValueManager.GetDateHeaderValues();
                responseHeaders.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);
            }

            return Output.WriteHeadersAsync(StreamId, StatusCode, responseHeaders);
        }

        public bool StatusCanHaveBody(int statusCode)
        {
            // List of status codes taken from Microsoft.Net.Http.Server.Response
            return statusCode != StatusCodes.Status204NoContent &&
                   statusCode != StatusCodes.Status205ResetContent &&
                   statusCode != StatusCodes.Status304NotModified;
        }

        private void ThrowResponseAlreadyStartedException(string value)
        {
            throw new InvalidOperationException(CoreStrings.FormatParameterReadOnlyAfterResponseStarted(value));
        }

        private void RejectNonBodyTransferEncodingResponse(bool appCompleted)
        {
            var ex = new InvalidOperationException(CoreStrings.FormatHeaderNotAllowedOnResponse("Transfer-Encoding", StatusCode));
            if (!appCompleted)
            {
                // Back out of header creation surface exeception in user code
                _requestProcessingStatus = RequestProcessingStatus.AppStarted;
                throw ex;
            }
            else
            {
                ReportApplicationError(ex);

                // 500 Internal Server Error
                SetErrorResponseHeaders(statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private void SetErrorResponseException(BadHttpRequestException ex)
        {
            SetErrorResponseHeaders(ex.StatusCode);

            if (!StringValues.IsNullOrEmpty(ex.AllowedHeader))
            {
                FrameResponseHeaders.HeaderAllow = ex.AllowedHeader;
            }
        }

        private void SetErrorResponseHeaders(int statusCode)
        {
            Debug.Assert(!HasResponseStarted, $"{nameof(SetErrorResponseHeaders)} called after response had already started.");

            StatusCode = statusCode;
            ReasonPhrase = null;

            var responseHeaders = FrameResponseHeaders;
            responseHeaders.Reset();
            var dateHeaderValues = DateHeaderValueManager.GetDateHeaderValues();

            responseHeaders.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);

            responseHeaders.ContentLength = 0;

            if (ServerOptions.AddServerHeader)
            {
                responseHeaders.SetRawServer(Constants.ServerName, _bytesServer);
            }
        }

        public void HandleNonBodyResponseWrite()
        {
            // Writes to HEAD response are ignored and logged at the end of the request
            if (Method != "HEAD")
            {
                // Throw Exception for 204, 205, 304 responses.
                throw new InvalidOperationException(CoreStrings.FormatWritingToResponseBodyNotSupported(StatusCode));
            }
        }

        private void ThrowResponseAbortedException()
        {
            throw new ObjectDisposedException(CoreStrings.UnhandledApplicationException, _applicationException);
        }

        public void ThrowRequestRejected(RequestRejectionReason reason)
            => throw BadHttpRequestException.GetException(reason);

        public void ThrowRequestRejected(RequestRejectionReason reason, string detail)
            => throw BadHttpRequestException.GetException(reason, detail);

        private void ThrowRequestTargetRejected(Span<byte> target)
            => throw GetInvalidRequestTargetException(target);

        private BadHttpRequestException GetInvalidRequestTargetException(Span<byte> target)
            => BadHttpRequestException.GetException(
                RequestRejectionReason.InvalidRequestTarget,
                Log.IsEnabled(LogLevel.Information)
                    ? target.GetAsciiStringEscaped(Constants.MaxExceptionDetailSize)
                    : string.Empty);

        public void SetBadRequestState(RequestRejectionReason reason)
        {
            SetBadRequestState(BadHttpRequestException.GetException(reason));
        }

        public void SetBadRequestState(BadHttpRequestException ex)
        {
            Log.ConnectionBadRequest(ConnectionId, ex);

            if (!HasResponseStarted)
            {
                SetErrorResponseException(ex);
            }

            _requestRejectedException = ex;
        }

        protected void ReportApplicationError(Exception ex)
        {
            if (_applicationException == null)
            {
                _applicationException = ex;
            }
            else if (_applicationException is AggregateException)
            {
                _applicationException = new AggregateException(_applicationException, ex).Flatten();
            }
            else
            {
                _applicationException = new AggregateException(_applicationException, ex);
            }

            Log.ApplicationError(ConnectionId, TraceIdentifier, ex);
        }

        private void OnOriginFormTarget(HttpMethod method, Http.HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
        {
            Debug.Assert(target[0] == ByteForwardSlash, "Should only be called when path starts with /");

            _requestTargetForm = HttpRequestTarget.OriginForm;

            // URIs are always encoded/escaped to ASCII https://tools.ietf.org/html/rfc3986#page-11
            // Multibyte Internationalized Resource Identifiers (IRIs) are first converted to utf8;
            // then encoded/escaped to ASCII  https://www.ietf.org/rfc/rfc3987.txt "Mapping of IRIs to URIs"
            string requestUrlPath = null;
            string rawTarget = null;

            try
            {
                // Read raw target before mutating memory.
                rawTarget = target.GetAsciiStringNonNullCharacters();

                if (pathEncoded)
                {
                    // URI was encoded, unescape and then parse as UTF-8
                    var pathLength = UrlEncoder.Decode(path, path);

                    // Removing dot segments must be done after unescaping. From RFC 3986:
                    //
                    // URI producing applications should percent-encode data octets that
                    // correspond to characters in the reserved set unless these characters
                    // are specifically allowed by the URI scheme to represent data in that
                    // component.  If a reserved character is found in a URI component and
                    // no delimiting role is known for that character, then it must be
                    // interpreted as representing the data octet corresponding to that
                    // character's encoding in US-ASCII.
                    //
                    // https://tools.ietf.org/html/rfc3986#section-2.2
                    pathLength = PathNormalizer.RemoveDotSegments(path.Slice(0, pathLength));

                    requestUrlPath = GetUtf8String(path.Slice(0, pathLength));
                }
                else
                {
                    var pathLength = PathNormalizer.RemoveDotSegments(path);

                    if (path.Length == pathLength && query.Length == 0)
                    {
                        // If no decoding was required, no dot segments were removed and
                        // there is no query, the request path is the same as the raw target
                        requestUrlPath = rawTarget;
                    }
                    else
                    {
                        requestUrlPath = path.Slice(0, pathLength).GetAsciiStringNonNullCharacters();
                    }
                }
            }
            catch (InvalidOperationException)
            {
                ThrowRequestTargetRejected(target);
            }

            QueryString = query.GetAsciiStringNonNullCharacters();
            RawTarget = rawTarget;
            Path = requestUrlPath;
        }

        private void OnAuthorityFormTarget(HttpMethod method, Span<byte> target)
        {
            _requestTargetForm = HttpRequestTarget.AuthorityForm;

            // This is not complete validation. It is just a quick scan for invalid characters
            // but doesn't check that the target fully matches the URI spec.
            for (var i = 0; i < target.Length; i++)
            {
                var ch = target[i];
                if (!UriUtilities.IsValidAuthorityCharacter(ch))
                {
                    ThrowRequestTargetRejected(target);
                }
            }

            // The authority-form of request-target is only used for CONNECT
            // requests (https://tools.ietf.org/html/rfc7231#section-4.3.6).
            if (method != HttpMethod.Connect)
            {
                ThrowRequestRejected(RequestRejectionReason.ConnectMethodRequired);
            }

            // When making a CONNECT request to establish a tunnel through one or
            // more proxies, a client MUST send only the target URI's authority
            // component (excluding any userinfo and its "@" delimiter) as the
            // request-target.For example,
            //
            //  CONNECT www.example.com:80 HTTP/1.1
            //
            // Allowed characters in the 'host + port' section of authority.
            // See https://tools.ietf.org/html/rfc3986#section-3.2
            RawTarget = target.GetAsciiStringNonNullCharacters();
            Path = string.Empty;
            QueryString = string.Empty;
        }

        private void OnAsteriskFormTarget(HttpMethod method)
        {
            _requestTargetForm = HttpRequestTarget.AsteriskForm;

            // The asterisk-form of request-target is only used for a server-wide
            // OPTIONS request (https://tools.ietf.org/html/rfc7231#section-4.3.7).
            if (method != HttpMethod.Options)
            {
                ThrowRequestRejected(RequestRejectionReason.OptionsMethodRequired);
            }

            RawTarget = Asterisk;
            Path = string.Empty;
            QueryString = string.Empty;
        }

        private void OnAbsoluteFormTarget(Span<byte> target, Span<byte> query)
        {
            _requestTargetForm = HttpRequestTarget.AbsoluteForm;

            // absolute-form
            // https://tools.ietf.org/html/rfc7230#section-5.3.2

            // This code should be the edge-case.

            // From the spec:
            //    a server MUST accept the absolute-form in requests, even though
            //    HTTP/1.1 clients will only send them in requests to proxies.

            RawTarget = target.GetAsciiStringNonNullCharacters();

            // Validation of absolute URIs is slow, but clients
            // should not be sending this form anyways, so perf optimization
            // not high priority

            if (!Uri.TryCreate(RawTarget, UriKind.Absolute, out var uri))
            {
                ThrowRequestTargetRejected(target);
            }

            _absoluteRequestTarget = uri;
            Path = uri.LocalPath;
            // don't use uri.Query because we need the unescaped version
            QueryString = query.GetAsciiStringNonNullCharacters();
        }

        private unsafe static string GetUtf8String(Span<byte> path)
        {
            // .NET 451 doesn't have pointer overloads for Encoding.GetString so we
            // copy to an array
            fixed (byte* pointer = &path.DangerousGetPinnableReference())
            {
                return Encoding.UTF8.GetString(pointer, path.Length);
            }
        }

        public void OnHeader(Span<byte> name, Span<byte> value)
        {
            // TODO: move validation of header count and size to HPACK decoding
            var valueString = value.GetAsciiStringNonNullCharacters();

            FrameRequestHeaders.Append(name, valueString);
        }

        protected void EnsureHostHeaderExists()
        {
            // https://tools.ietf.org/html/rfc7230#section-5.4
            // A server MUST respond with a 400 (Bad Request) status code to any
            // HTTP/1.1 request message that lacks a Host header field and to any
            // request message that contains more than one Host header field or a
            // Host header field with an invalid field-value.

            var host = FrameRequestHeaders.HeaderHost;
            if (host.Count <= 0)
            {
                ThrowRequestRejected(RequestRejectionReason.MissingHostHeader);
            }
            else if (host.Count > 1)
            {
                ThrowRequestRejected(RequestRejectionReason.MultipleHostHeaders);
            }
            else if (_requestTargetForm == HttpRequestTarget.AuthorityForm)
            {
                if (!host.Equals(RawTarget))
                {
                    ThrowRequestRejected(RequestRejectionReason.InvalidHostHeader, host.ToString());
                }
            }
            else if (_requestTargetForm == HttpRequestTarget.AbsoluteForm)
            {
                // If the target URI includes an authority component, then a
                // client MUST send a field - value for Host that is identical to that
                // authority component, excluding any userinfo subcomponent and its "@"
                // delimiter.

                // System.Uri doesn't not tell us if the port was in the original string or not.
                // When IsDefaultPort = true, we will allow Host: with or without the default port
                var authorityAndPort = _absoluteRequestTarget.Authority + ":" + _absoluteRequestTarget.Port;
                if ((host != _absoluteRequestTarget.Authority || !_absoluteRequestTarget.IsDefaultPort)
                    && host != authorityAndPort)
                {
                    ThrowRequestRejected(RequestRejectionReason.InvalidHostHeader, host.ToString());
                }
            }
        }

        private IPipe CreateRequestBodyPipe()
            => _context.PipeFactory.Create(new PipeOptions
            {
                ReaderScheduler = ServiceContext.ThreadPool,
                WriterScheduler = InlineScheduler.Default,
                MaximumSizeHigh = 1,
                MaximumSizeLow = 1
            });

        private enum HttpRequestTarget
        {
            Unknown = -1,
            // origin-form is the most common
            OriginForm,
            AbsoluteForm,
            AuthorityForm,
            AsteriskForm
        }
    }
}
