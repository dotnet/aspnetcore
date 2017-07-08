// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.Text.Encodings.Web.Utf8;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

// ReSharper disable AccessToModifiedClosure

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public abstract partial class Frame : IFrameControl
    {
        private const byte ByteAsterisk = (byte)'*';
        private const byte ByteForwardSlash = (byte)'/';
        private const byte BytePercentage = (byte)'%';

        private static readonly ArraySegment<byte> _endChunkedResponseBytes = CreateAsciiByteArraySegment("0\r\n\r\n");
        private static readonly ArraySegment<byte> _continueBytes = CreateAsciiByteArraySegment("HTTP/1.1 100 Continue\r\n\r\n");
        private static readonly Action<WritableBuffer, FrameAdapter> _writeHeaders = WriteResponseHeaders;

        private static readonly byte[] _bytesConnectionClose = Encoding.ASCII.GetBytes("\r\nConnection: close");
        private static readonly byte[] _bytesConnectionKeepAlive = Encoding.ASCII.GetBytes("\r\nConnection: keep-alive");
        private static readonly byte[] _bytesTransferEncodingChunked = Encoding.ASCII.GetBytes("\r\nTransfer-Encoding: chunked");
        private static readonly byte[] _bytesHttpVersion11 = Encoding.ASCII.GetBytes("HTTP/1.1 ");
        private static readonly byte[] _bytesEndHeaders = Encoding.ASCII.GetBytes("\r\n\r\n");
        private static readonly byte[] _bytesServer = Encoding.ASCII.GetBytes("\r\nServer: " + Constants.ServerName);

        private const string EmptyPath = "/";
        private const string Asterisk = "*";

        private readonly object _onStartingSync = new Object();
        private readonly object _onCompletedSync = new Object();

        private Streams _frameStreams;

        protected Stack<KeyValuePair<Func<object, Task>, object>> _onStarting;
        protected Stack<KeyValuePair<Func<object, Task>, object>> _onCompleted;

        protected volatile bool _requestProcessingStopping; // volatile, see: https://msdn.microsoft.com/en-us/library/x13ttww7.aspx
        protected int _requestAborted;
        private CancellationTokenSource _abortedCts;
        private CancellationToken? _manuallySetRequestAbortToken;

        protected RequestProcessingStatus _requestProcessingStatus;
        protected bool _keepAlive;
        protected bool _upgradeAvailable;
        private volatile bool _wasUpgraded;
        private bool _canHaveBody;
        private bool _autoChunk;
        protected Exception _applicationException;
        private BadHttpRequestException _requestRejectedException;

        protected HttpVersion _httpVersion;

        private string _requestId;
        private int _remainingRequestHeadersBytesAllowed;
        private int _requestHeadersParsed;
        private uint _requestCount;

        protected readonly long _keepAliveTicks;
        private readonly long _requestHeadersTimeoutTicks;

        protected long _responseBytesWritten;

        private readonly FrameContext _frameContext;
        private readonly IHttpParser<FrameAdapter> _parser;

        private HttpRequestTarget _requestTargetForm = HttpRequestTarget.Unknown;
        private Uri _absoluteRequestTarget;

        public Frame(FrameContext frameContext)
        {
            _frameContext = frameContext;

            ServerOptions = ServiceContext.ServerOptions;

            _parser = ServiceContext.HttpParserFactory(new FrameAdapter(this));

            FrameControl = this;
            _keepAliveTicks = ServerOptions.Limits.KeepAliveTimeout.Ticks;
            _requestHeadersTimeoutTicks = ServerOptions.Limits.RequestHeadersTimeout.Ticks;

            Output = new OutputProducer(frameContext.Output, frameContext.ConnectionId, frameContext.ServiceContext.Log, TimeoutControl);
            RequestBodyPipe = CreateRequestBodyPipe();
        }

        public IPipe RequestBodyPipe { get; }

        public ServiceContext ServiceContext => _frameContext.ServiceContext;
        public IConnectionInformation ConnectionInformation => _frameContext.ConnectionInformation;

        public IFeatureCollection ConnectionFeatures { get; set; }
        public IPipeReader Input => _frameContext.Input;
        public OutputProducer Output { get; }
        public ITimeoutControl TimeoutControl => _frameContext.TimeoutControl;

        protected IKestrelTrace Log => ServiceContext.Log;
        private DateHeaderValueManager DateHeaderValueManager => ServiceContext.DateHeaderValueManager;
        // Hold direct reference to ServerOptions since this is used very often in the request processing path
        private KestrelServerOptions ServerOptions { get; }
        private IPEndPoint LocalEndPoint => ConnectionInformation.LocalEndPoint;
        private IPEndPoint RemoteEndPoint => ConnectionInformation.RemoteEndPoint;
        protected string ConnectionId => _frameContext.ConnectionId;

        public string ConnectionIdFeature { get; set; }
        public bool HasStartedConsumingRequestBody { get; set; }
        public long? MaxRequestBodySize { get; set; }
        public bool AllowSynchronousIO { get; set; }

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
                    _requestId = StringUtilities.ConcatAsHexSuffix(ConnectionId, ':', _requestCount);
                }
                return _requestId;
            }
        }

        public bool WasUpgraded => _wasUpgraded;
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

        public string HttpVersion
        {
            get
            {
                if (_httpVersion == Http.HttpVersion.Http11)
                {
                    return "HTTP/1.1";
                }
                if (_httpVersion == Http.HttpVersion.Http10)
                {
                    return "HTTP/1.0";
                }

                return string.Empty;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                // GetKnownVersion returns versions which ReferenceEquals interned string
                // As most common path, check for this only in fast-path and inline
                if (ReferenceEquals(value, "HTTP/1.1"))
                {
                    _httpVersion = Http.HttpVersion.Http11;
                }
                else if (ReferenceEquals(value, "HTTP/1.0"))
                {
                    _httpVersion = Http.HttpVersion.Http10;
                }
                else
                {
                    HttpVersionSetSlow(value);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void HttpVersionSetSlow(string value)
        {
            if (value == "HTTP/1.1")
            {
                _httpVersion = Http.HttpVersion.Http11;
            }
            else if (value == "HTTP/1.0")
            {
                _httpVersion = Http.HttpVersion.Http10;
            }
            else
            {
                _httpVersion = Http.HttpVersion.Unknown;
            }
        }

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

        public MinDataRate MinRequestBodyDataRate { get; set; }

        public MinDataRate MinResponseDataRate { get; set; }

        public void InitializeStreams(MessageBody messageBody)
        {
            if (_frameStreams == null)
            {
                _frameStreams = new Streams(bodyControl: this, frameControl: this);
            }

            (RequestBody, ResponseBody) = _frameStreams.Start(messageBody);
        }

        public void PauseStreams() => _frameStreams.Pause();

        public void StopStreams() => _frameStreams.Stop();

        public void Reset()
        {
            _onStarting = null;
            _onCompleted = null;

            _requestProcessingStatus = RequestProcessingStatus.RequestPending;
            _keepAlive = false;
            _autoChunk = false;
            _applicationException = null;

            ResetFeatureCollection();

            HasStartedConsumingRequestBody = false;
            MaxRequestBodySize = ServerOptions.Limits.MaxRequestBodySize;
            AllowSynchronousIO = ServerOptions.AllowSynchronousIO;
            TraceIdentifier = null;
            Scheme = null;
            Method = null;
            PathBase = null;
            Path = null;
            RawTarget = null;
            _requestTargetForm = HttpRequestTarget.Unknown;
            _absoluteRequestTarget = null;
            QueryString = null;
            _httpVersion = Http.HttpVersion.Unknown;
            StatusCode = StatusCodes.Status200OK;
            ReasonPhrase = null;

            RemoteIpAddress = RemoteEndPoint?.Address;
            RemotePort = RemoteEndPoint?.Port ?? 0;

            LocalIpAddress = LocalEndPoint?.Address;
            LocalPort = LocalEndPoint?.Port ?? 0;
            ConnectionIdFeature = ConnectionId;

            FrameRequestHeaders.Reset();
            FrameResponseHeaders.Reset();
            RequestHeaders = FrameRequestHeaders;
            ResponseHeaders = FrameResponseHeaders;

            if (ConnectionFeatures != null)
            {
                foreach (var feature in ConnectionFeatures)
                {
                    // Set the scheme to https if there's an ITlsConnectionFeature
                    if (feature.Key == typeof(ITlsConnectionFeature))
                    {
                        Scheme = "https";
                    }

                    FastFeatureSet(feature.Key, feature.Value);
                }
            }

            _manuallySetRequestAbortToken = null;
            _abortedCts = null;

            // Allow two bytes for \r\n after headers
            _remainingRequestHeadersBytesAllowed = ServerOptions.Limits.MaxRequestHeadersTotalSize + 2;
            _requestHeadersParsed = 0;

            _responseBytesWritten = 0;
            _requestCount++;

            MinRequestBodyDataRate = ServerOptions.Limits.MinRequestBodyDataRate;
            MinResponseDataRate = ServerOptions.Limits.MinResponseDataRate;
        }

        /// <summary>
        /// Stops the request processing loop between requests.
        /// Called on all active connections when the server wants to initiate a shutdown
        /// and after a keep-alive timeout.
        /// </summary>
        public void Stop()
        {
            _requestProcessingStopping = true;
            Input.CancelPendingRead();
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

        /// <summary>
        /// Immediate kill the connection and poison the request and response streams.
        /// </summary>
        public void Abort(Exception error)
        {
            if (Interlocked.Exchange(ref _requestAborted, 1) == 0)
            {
                _requestProcessingStopping = true;

                _frameStreams?.Abort(error);

                Output.Abort(error);

                // Potentially calling user code. CancelRequestAbortedToken logs any exceptions.
                ServiceContext.ThreadPool.UnsafeRun(state => ((Frame)state).CancelRequestAbortedToken(), this);
            }
        }

        /// <summary>
        /// Primary loop which consumes socket input, parses it for protocol framing, and invokes the
        /// application delegate for as long as the socket is intended to remain open.
        /// The resulting Task from this loop is preserved in a field which is used when the server needs
        /// to drain and close all currently active connections.
        /// </summary>
        public abstract Task ProcessRequestsAsync();

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
                if (_autoChunk)
                {
                    if (data.Count == 0)
                    {
                        return Task.CompletedTask;
                    }
                    return WriteChunkedAsync(data, cancellationToken);
                }
                else
                {
                    CheckLastWrite();
                    return Output.WriteAsync(data, cancellationToken: cancellationToken);
                }
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
                if (_autoChunk)
                {
                    if (data.Count == 0)
                    {
                        await FlushAsync(cancellationToken);
                        return;
                    }

                    await WriteChunkedAsync(data, cancellationToken);
                }
                else
                {
                    CheckLastWrite();
                    await Output.WriteAsync(data, cancellationToken: cancellationToken);
                }
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
                _keepAlive = false;
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
                    _keepAlive = false;
                }

                ReportApplicationError(new InvalidOperationException(
                    CoreStrings.FormatTooFewBytesWritten(_responseBytesWritten, responseHeaders.ContentLength.Value)));
            }
        }

        private Task WriteChunkedAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            return Output.WriteAsync(data, chunk: true, cancellationToken: cancellationToken);
        }

        private Task WriteChunkedResponseSuffix()
        {
            return Output.WriteAsync(_endChunkedResponseBytes);
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

            if (_httpVersion == Http.HttpVersion.Http11 &&
                RequestHeaders.TryGetValue("Expect", out var expect) &&
                (expect.FirstOrDefault() ?? "").Equals("100-continue", StringComparison.OrdinalIgnoreCase))
            {
                Output.WriteAsync(_continueBytes).GetAwaiter().GetResult();
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
            ProduceStart(appCompleted: false);

            return Task.CompletedTask;
        }

        private async Task InitializeResponseAwaited(int firstWriteByteCount)
        {
            await FireOnStarting();

            if (_applicationException != null)
            {
                ThrowResponseAbortedException();
            }

            VerifyAndUpdateWrite(firstWriteByteCount);
            ProduceStart(appCompleted: false);
        }

        private void ProduceStart(bool appCompleted)
        {
            if (HasResponseStarted)
            {
                return;
            }

            _requestProcessingStatus = RequestProcessingStatus.ResponseStarted;

            CreateResponseHeader(appCompleted);
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
                    _requestProcessingStopping = true;
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
            ProduceStart(appCompleted: true);

            // Force flush
            await Output.FlushAsync();

            await WriteSuffix();
        }

        private Task WriteSuffix()
        {
            // _autoChunk should be checked after we are sure ProduceStart() has been called
            // since ProduceStart() may set _autoChunk to true.
            if (_autoChunk)
            {
                return WriteAutoChunkSuffixAwaited();
            }

            if (_keepAlive)
            {
                Log.ConnectionKeepAlive(ConnectionId);
            }

            if (HttpMethods.IsHead(Method) && _responseBytesWritten > 0)
            {
                Log.ConnectionHeadResponseBodyWrite(ConnectionId, _responseBytesWritten);
            }

            return Task.CompletedTask;
        }

        private async Task WriteAutoChunkSuffixAwaited()
        {
            // For the same reason we call CheckLastWrite() in Content-Length responses.
            _abortedCts = null;

            await WriteChunkedResponseSuffix();

            if (_keepAlive)
            {
                Log.ConnectionKeepAlive(ConnectionId);
            }
        }

        private void CreateResponseHeader(bool appCompleted)
        {
            var responseHeaders = FrameResponseHeaders;
            var hasConnection = responseHeaders.HasConnection;
            var connectionOptions = FrameHeaders.ParseConnection(responseHeaders.HeaderConnection);
            var hasTransferEncoding = responseHeaders.HasTransferEncoding;
            var transferCoding = FrameHeaders.GetFinalTransferCoding(responseHeaders.HeaderTransferEncoding);

            if (_keepAlive && hasConnection)
            {
                _keepAlive = (connectionOptions & ConnectionOptions.KeepAlive) == ConnectionOptions.KeepAlive;
            }

            // https://tools.ietf.org/html/rfc7230#section-3.3.1
            // If any transfer coding other than
            // chunked is applied to a response payload body, the sender MUST either
            // apply chunked as the final transfer coding or terminate the message
            // by closing the connection.
            if (hasTransferEncoding && transferCoding != TransferCoding.Chunked)
            {
                _keepAlive = false;
            }

            // Set whether response can have body
            _canHaveBody = StatusCanHaveBody(StatusCode) && Method != "HEAD";

            // Don't set the Content-Length or Transfer-Encoding headers
            // automatically for HEAD requests or 204, 205, 304 responses.
            if (_canHaveBody)
            {
                if (!hasTransferEncoding && !responseHeaders.ContentLength.HasValue)
                {
                    if (appCompleted && StatusCode != StatusCodes.Status101SwitchingProtocols)
                    {
                        // Since the app has completed and we are only now generating
                        // the headers we can safely set the Content-Length to 0.
                        responseHeaders.ContentLength = 0;
                    }
                    else
                    {
                        // Note for future reference: never change this to set _autoChunk to true on HTTP/1.0
                        // connections, even if we were to infer the client supports it because an HTTP/1.0 request
                        // was received that used chunked encoding. Sending a chunked response to an HTTP/1.0
                        // client would break compliance with RFC 7230 (section 3.3.1):
                        //
                        // A server MUST NOT send a response containing Transfer-Encoding unless the corresponding
                        // request indicates HTTP/1.1 (or later).
                        if (_httpVersion == Http.HttpVersion.Http11 && StatusCode != StatusCodes.Status101SwitchingProtocols)
                        {
                            _autoChunk = true;
                            responseHeaders.SetRawTransferEncoding("chunked", _bytesTransferEncodingChunked);
                        }
                        else
                        {
                            _keepAlive = false;
                        }
                    }
                }
            }
            else if (hasTransferEncoding)
            {
                RejectNonBodyTransferEncodingResponse(appCompleted);
            }

            responseHeaders.SetReadOnly();

            if (!hasConnection)
            {
                if (!_keepAlive)
                {
                    responseHeaders.SetRawConnection("close", _bytesConnectionClose);
                }
                else if (_httpVersion == Http.HttpVersion.Http10)
                {
                    responseHeaders.SetRawConnection("keep-alive", _bytesConnectionKeepAlive);
                }
            }

            if (ServerOptions.AddServerHeader && !responseHeaders.HasServer)
            {
                responseHeaders.SetRawServer(Constants.ServerName, _bytesServer);
            }

            if (!responseHeaders.HasDate)
            {
                var dateHeaderValues = DateHeaderValueManager.GetDateHeaderValues();
                responseHeaders.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);
            }

            Output.Write(_writeHeaders, new FrameAdapter(this));
        }

        private static void WriteResponseHeaders(WritableBuffer writableBuffer, FrameAdapter frameAdapter)
        {
            var frame = frameAdapter.Frame;
            var writer = new WritableBufferWriter(writableBuffer);

            var responseHeaders = frame.FrameResponseHeaders;
            writer.Write(_bytesHttpVersion11);
            var statusBytes = ReasonPhrases.ToStatusBytes(frame.StatusCode, frame.ReasonPhrase);
            writer.Write(statusBytes);
            responseHeaders.CopyTo(ref writer);
            writer.Write(_bytesEndHeaders);
        }

        public void ParseRequest(ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined)
        {
            consumed = buffer.Start;
            examined = buffer.End;

            switch (_requestProcessingStatus)
            {
                case RequestProcessingStatus.RequestPending:
                    if (buffer.IsEmpty)
                    {
                        break;
                    }

                    TimeoutControl.ResetTimeout(_requestHeadersTimeoutTicks, TimeoutAction.SendTimeoutResponse);

                    _requestProcessingStatus = RequestProcessingStatus.ParsingRequestLine;
                    goto case RequestProcessingStatus.ParsingRequestLine;
                case RequestProcessingStatus.ParsingRequestLine:
                    if (TakeStartLine(buffer, out consumed, out examined))
                    {
                        buffer = buffer.Slice(consumed, buffer.End);

                        _requestProcessingStatus = RequestProcessingStatus.ParsingHeaders;
                        goto case RequestProcessingStatus.ParsingHeaders;
                    }
                    else
                    {
                        break;
                    }
                case RequestProcessingStatus.ParsingHeaders:
                    if (TakeMessageHeaders(buffer, out consumed, out examined))
                    {
                        _requestProcessingStatus = RequestProcessingStatus.AppStarted;
                    }
                    break;
            }
        }

        public bool TakeStartLine(ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined)
        {
            var overLength = false;
            if (buffer.Length >= ServerOptions.Limits.MaxRequestLineSize)
            {
                buffer = buffer.Slice(buffer.Start, ServerOptions.Limits.MaxRequestLineSize);
                overLength = true;
            }

            var result = _parser.ParseRequestLine(new FrameAdapter(this), buffer, out consumed, out examined);
            if (!result && overLength)
            {
                ThrowRequestRejected(RequestRejectionReason.RequestLineTooLong);
            }

            return result;
        }

        public bool TakeMessageHeaders(ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined)
        {
            // Make sure the buffer is limited
            bool overLength = false;
            if (buffer.Length >= _remainingRequestHeadersBytesAllowed)
            {
                buffer = buffer.Slice(buffer.Start, _remainingRequestHeadersBytesAllowed);

                // If we sliced it means the current buffer bigger than what we're
                // allowed to look at
                overLength = true;
            }

            var result = _parser.ParseHeaders(new FrameAdapter(this), buffer, out consumed, out examined, out var consumedBytes);
            _remainingRequestHeadersBytesAllowed -= consumedBytes;

            if (!result && overLength)
            {
                ThrowRequestRejected(RequestRejectionReason.HeadersExceedMaxTotalSize);
            }
            if (result)
            {
                TimeoutControl.CancelTimeout();
            }
            return result;
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

            _keepAlive = false;
            _requestProcessingStopping = true;
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

        public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
        {
            Debug.Assert(target.Length != 0, "Request target must be non-zero length");

            var ch = target[0];
            if (ch == ByteForwardSlash)
            {
                // origin-form.
                // The most common form of request-target.
                // https://tools.ietf.org/html/rfc7230#section-5.3.1
                OnOriginFormTarget(method, version, target, path, query, customMethod, pathEncoded);
            }
            else if (ch == ByteAsterisk && target.Length == 1)
            {
                OnAsteriskFormTarget(method);
            }
            else if (target.GetKnownHttpScheme(out var scheme))
            {
                OnAbsoluteFormTarget(target, query);
            }
            else
            {
                // Assume anything else is considered authority form.
                // FYI: this should be an edge case. This should only happen when
                // a client mistakenly thinks this server is a proxy server.
                OnAuthorityFormTarget(method, target);
            }

            Method = method != HttpMethod.Custom
                ? HttpUtilities.MethodToString(method) ?? string.Empty
                : customMethod.GetAsciiStringNonNullCharacters();
            HttpVersion = HttpUtilities.VersionToString(version);

            Debug.Assert(RawTarget != null, "RawTarget was not set");
            Debug.Assert(Method != null, "Method was not set");
            Debug.Assert(Path != null, "Path was not set");
            Debug.Assert(QueryString != null, "QueryString was not set");
            Debug.Assert(HttpVersion != null, "HttpVersion was not set");
        }

        private void OnOriginFormTarget(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
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
            _requestHeadersParsed++;
            if (_requestHeadersParsed > ServerOptions.Limits.MaxRequestHeaderCount)
            {
                ThrowRequestRejected(RequestRejectionReason.TooManyHeaders);
            }
            var valueString = value.GetAsciiStringNonNullCharacters();

            FrameRequestHeaders.Append(name, valueString);
        }

        protected void EnsureHostHeaderExists()
        {
            if (_httpVersion == Http.HttpVersion.Http10)
            {
                return;
            }

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
            => ConnectionInformation.PipeFactory.Create(new PipeOptions
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
