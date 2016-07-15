// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

// ReSharper disable AccessToModifiedClosure

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public abstract partial class Frame : ConnectionContext, IFrameControl
    {
        private static readonly ArraySegment<byte> _endChunkedResponseBytes = CreateAsciiByteArraySegment("0\r\n\r\n");
        private static readonly ArraySegment<byte> _continueBytes = CreateAsciiByteArraySegment("HTTP/1.1 100 Continue\r\n\r\n");
        private static readonly ArraySegment<byte> _emptyData = new ArraySegment<byte>(new byte[0]);

        private static readonly byte[] _bytesConnectionClose = Encoding.ASCII.GetBytes("\r\nConnection: close");
        private static readonly byte[] _bytesConnectionKeepAlive = Encoding.ASCII.GetBytes("\r\nConnection: keep-alive");
        private static readonly byte[] _bytesTransferEncodingChunked = Encoding.ASCII.GetBytes("\r\nTransfer-Encoding: chunked");
        private static readonly byte[] _bytesHttpVersion11 = Encoding.ASCII.GetBytes("HTTP/1.1 ");
        private static readonly byte[] _bytesContentLengthZero = Encoding.ASCII.GetBytes("\r\nContent-Length: 0");
        private static readonly byte[] _bytesEndHeaders = Encoding.ASCII.GetBytes("\r\n\r\n");
        private static readonly byte[] _bytesServer = Encoding.ASCII.GetBytes("\r\nServer: Kestrel");

        private static Vector<byte> _vectorCRs = new Vector<byte>((byte)'\r');
        private static Vector<byte> _vectorColons = new Vector<byte>((byte)':');
        private static Vector<byte> _vectorSpaces = new Vector<byte>((byte)' ');
        private static Vector<byte> _vectorTabs = new Vector<byte>((byte)'\t');
        private static Vector<byte> _vectorQuestionMarks = new Vector<byte>((byte)'?');
        private static Vector<byte> _vectorPercentages = new Vector<byte>((byte)'%');

        private readonly object _onStartingSync = new Object();
        private readonly object _onCompletedSync = new Object();

        protected bool _requestRejected;
        private Streams _frameStreams;

        protected List<KeyValuePair<Func<object, Task>, object>> _onStarting;

        protected List<KeyValuePair<Func<object, Task>, object>> _onCompleted;

        private Task _requestProcessingTask;
        protected volatile bool _requestProcessingStopping; // volatile, see: https://msdn.microsoft.com/en-us/library/x13ttww7.aspx
        protected int _requestAborted;
        private CancellationTokenSource _abortedCts;
        private CancellationToken? _manuallySetRequestAbortToken;

        protected RequestProcessingStatus _requestProcessingStatus;
        protected bool _keepAlive;
        private bool _autoChunk;
        protected Exception _applicationException;

        private HttpVersionType _httpVersion;

        private readonly string _pathBase;

        public Frame(ConnectionContext context)
            : base(context)
        {
            _pathBase = context.ServerAddress.PathBase;

            FrameControl = this;
            Reset();
        }

        public string ConnectionIdFeature { get; set; }
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
                if (_httpVersion == HttpVersionType.Http11)
                {
                    return "HTTP/1.1";
                }
                if (_httpVersion == HttpVersionType.Http10)
                {
                    return "HTTP/1.0";
                }

                return string.Empty;
            }
            set
            {
                if (value == "HTTP/1.1")
                {
                    _httpVersion = HttpVersionType.Http11;
                }
                else if (value == "HTTP/1.0")
                {
                    _httpVersion = HttpVersionType.Http10;
                }
                else
                {
                    _httpVersion = HttpVersionType.Unset;
                }
            }
        }

        public IHeaderDictionary RequestHeaders { get; set; }
        public Stream RequestBody { get; set; }

        private int _statusCode;
        public int StatusCode
        {
            get
            {
                return _statusCode;
            }
            set
            {
                if (HasResponseStarted)
                {
                    throw new InvalidOperationException("Status code cannot be set, response has already started.");
                }

                _statusCode = value;
            }
        }

        private string _reasonPhrase;
        public string ReasonPhrase
        {
            get
            {
                return _reasonPhrase;
            }
            set
            {
                if (HasResponseStarted)
                {
                    throw new InvalidOperationException("Reason phrase cannot be set, response had already started.");
                }

                _reasonPhrase = value;
            }
        }

        public IHeaderDictionary ResponseHeaders { get; set; }
        public Stream ResponseBody { get; set; }

        public Stream DuplexStream { get; set; }

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

        public bool HasResponseStarted
        {
            get { return _requestProcessingStatus == RequestProcessingStatus.ResponseStarted; }
        }

        protected FrameRequestHeaders FrameRequestHeaders { get; private set; }

        protected FrameResponseHeaders FrameResponseHeaders { get; private set; }

        public void InitializeHeaders()
        {
            if (FrameRequestHeaders == null)
            {
                FrameRequestHeaders = new FrameRequestHeaders();
            }

            RequestHeaders = FrameRequestHeaders;

            if (FrameResponseHeaders == null)
            {
                FrameResponseHeaders = new FrameResponseHeaders();
            }

            ResponseHeaders = FrameResponseHeaders;
        }

        public void InitializeStreams(MessageBody messageBody)
        {
            if (_frameStreams == null)
            {
                _frameStreams = new Streams(this);
            }

            RequestBody = _frameStreams.RequestBody;
            ResponseBody = _frameStreams.ResponseBody;
            DuplexStream = _frameStreams.DuplexStream;

            _frameStreams.RequestBody.StartAcceptingReads(messageBody);
            _frameStreams.ResponseBody.StartAcceptingWrites();
        }

        public void PauseStreams()
        {
            _frameStreams.RequestBody.PauseAcceptingReads();
            _frameStreams.ResponseBody.PauseAcceptingWrites();
        }

        public void ResumeStreams()
        {
            _frameStreams.RequestBody.ResumeAcceptingReads();
            _frameStreams.ResponseBody.ResumeAcceptingWrites();
        }

        public void StopStreams()
        {
            _frameStreams.RequestBody.StopAcceptingReads();
            _frameStreams.ResponseBody.StopAcceptingWrites();
        }

        public void Reset()
        {
            FrameRequestHeaders?.Reset();
            FrameResponseHeaders?.Reset();

            _onStarting = null;
            _onCompleted = null;

            _requestProcessingStatus = RequestProcessingStatus.RequestPending;
            _keepAlive = false;
            _autoChunk = false;
            _applicationException = null;

            ResetFeatureCollection();

            Scheme = null;
            Method = null;
            PathBase = null;
            Path = null;
            QueryString = null;
            _httpVersion = HttpVersionType.Unset;
            StatusCode = 200;
            ReasonPhrase = null;

            RemoteIpAddress = RemoteEndPoint?.Address;
            RemotePort = RemoteEndPoint?.Port ?? 0;

            LocalIpAddress = LocalEndPoint?.Address;
            LocalPort = LocalEndPoint?.Port ?? 0;
            ConnectionIdFeature = ConnectionId;

            PrepareRequest?.Invoke(this);

            _manuallySetRequestAbortToken = null;
            _abortedCts = null;
        }

        /// <summary>
        /// Called once by Connection class to begin the RequestProcessingAsync loop.
        /// </summary>
        public void Start()
        {
            _requestProcessingTask =
                Task.Factory.StartNew(
                    (o) => ((Frame)o).RequestProcessingAsync(),
                    this,
                    default(CancellationToken),
                    TaskCreationOptions.DenyChildAttach,
                    TaskScheduler.Default);
        }

        /// <summary>
        /// Should be called when the server wants to initiate a shutdown. The Task returned will
        /// become complete when the RequestProcessingAsync function has exited. It is expected that
        /// Stop will be called on all active connections, and Task.WaitAll() will be called on every
        /// return value.
        /// </summary>
        public Task Stop()
        {
            if (!_requestProcessingStopping)
            {
                _requestProcessingStopping = true;
            }
            return _requestProcessingTask ?? TaskUtilities.CompletedTask;
        }

        /// <summary>
        /// Immediate kill the connection and poison the request and response streams.
        /// </summary>
        public void Abort(Exception error = null)
        {
            if (Interlocked.CompareExchange(ref _requestAborted, 1, 0) == 0)
            {
                _requestProcessingStopping = true;

                _frameStreams?.RequestBody.Abort(error);
                _frameStreams?.ResponseBody.Abort();

                try
                {
                    ConnectionControl.End(ProduceEndType.SocketDisconnect);
                }
                catch (Exception ex)
                {
                    Log.LogError(0, ex, "Abort");
                }

                try
                {
                    RequestAbortedSource.Cancel();
                }
                catch (Exception ex)
                {
                    Log.LogError(0, ex, "Abort");
                }
                _abortedCts = null;
            }
        }

        /// <summary>
        /// Primary loop which consumes socket input, parses it for protocol framing, and invokes the
        /// application delegate for as long as the socket is intended to remain open.
        /// The resulting Task from this loop is preserved in a field which is used when the server needs
        /// to drain and close all currently active connections.
        /// </summary>
        public abstract Task RequestProcessingAsync();

        public void OnStarting(Func<object, Task> callback, object state)
        {
            lock (_onStartingSync)
            {
                if (_onStarting == null)
                {
                    _onStarting = new List<KeyValuePair<Func<object, Task>, object>>();
                }
                _onStarting.Add(new KeyValuePair<Func<object, Task>, object>(callback, state));
            }
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            lock (_onCompletedSync)
            {
                if (_onCompleted == null)
                {
                    _onCompleted = new List<KeyValuePair<Func<object, Task>, object>>();
                }
                _onCompleted.Add(new KeyValuePair<Func<object, Task>, object>(callback, state));
            }
        }

        protected async Task FireOnStarting()
        {
            List<KeyValuePair<Func<object, Task>, object>> onStarting = null;
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
            List<KeyValuePair<Func<object, Task>, object>> onCompleted = null;
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

        public void Flush()
        {
            ProduceStartAndFireOnStarting().GetAwaiter().GetResult();
            SocketOutput.Write(_emptyData);
        }

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            await ProduceStartAndFireOnStarting();
            await SocketOutput.WriteAsync(_emptyData, cancellationToken: cancellationToken);
        }

        public void Write(ArraySegment<byte> data)
        {
            ProduceStartAndFireOnStarting().GetAwaiter().GetResult();

            if (_autoChunk)
            {
                if (data.Count == 0)
                {
                    return;
                }
                WriteChunked(data);
            }
            else
            {
                SocketOutput.Write(data);
            }
        }

        public Task WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            if (!HasResponseStarted)
            {
                return WriteAsyncAwaited(data, cancellationToken);
            }

            if (_autoChunk)
            {
                if (data.Count == 0)
                {
                    return TaskUtilities.CompletedTask;
                }
                return WriteChunkedAsync(data, cancellationToken);
            }
            else
            {
                return SocketOutput.WriteAsync(data, cancellationToken: cancellationToken);
            }
        }

        public async Task WriteAsyncAwaited(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            await ProduceStartAndFireOnStarting();

            if (_autoChunk)
            {
                if (data.Count == 0)
                {
                    return;
                }
                await WriteChunkedAsync(data, cancellationToken);
            }
            else
            {
                await SocketOutput.WriteAsync(data, cancellationToken: cancellationToken);
            }
        }

        private void WriteChunked(ArraySegment<byte> data)
        {
            SocketOutput.Write(data, chunk: true);
        }

        private Task WriteChunkedAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            return SocketOutput.WriteAsync(data, chunk: true, cancellationToken: cancellationToken);
        }

        private Task WriteChunkedResponseSuffix()
        {
            return SocketOutput.WriteAsync(_endChunkedResponseBytes);
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

            StringValues expect;
            if (_httpVersion == HttpVersionType.Http11 &&
                RequestHeaders.TryGetValue("Expect", out expect) &&
                (expect.FirstOrDefault() ?? "").Equals("100-continue", StringComparison.OrdinalIgnoreCase))
            {
                SocketOutput.Write(_continueBytes);
            }
        }

        public Task ProduceStartAndFireOnStarting()
        {
            if (HasResponseStarted)
            {
                return TaskUtilities.CompletedTask;
            }

            if (_onStarting != null)
            {
                return ProduceStartAndFireOnStartingAwaited();
            }

            if (_applicationException != null)
            {
                throw new ObjectDisposedException(
                    "The response has been aborted due to an unhandled application exception.",
                    _applicationException);
            }

            ProduceStart(appCompleted: false);

            return TaskUtilities.CompletedTask;
        }

        private async Task ProduceStartAndFireOnStartingAwaited()
        {
            await FireOnStarting();

            if (_applicationException != null)
            {
                throw new ObjectDisposedException(
                    "The response has been aborted due to an unhandled application exception.",
                    _applicationException);
            }

            ProduceStart(appCompleted: false);
        }

        private void ProduceStart(bool appCompleted)
        {
            if (HasResponseStarted)
            {
                return;
            }

            _requestProcessingStatus = RequestProcessingStatus.ResponseStarted;

            var statusBytes = ReasonPhrases.ToStatusBytes(StatusCode, ReasonPhrase);

            CreateResponseHeader(statusBytes, appCompleted);
        }

        protected Task TryProduceInvalidRequestResponse()
        {
            if (_requestProcessingStatus == RequestProcessingStatus.RequestStarted && _requestRejected)
            {
                if (FrameRequestHeaders == null || FrameResponseHeaders == null)
                {
                    InitializeHeaders();
                }

                return ProduceEnd();
            }

            return TaskUtilities.CompletedTask;
        }

        protected Task ProduceEnd()
        {
            if (_requestRejected || _applicationException != null)
            {
                if (HasResponseStarted)
                {
                    // We can no longer change the response, so we simply close the connection.
                    _requestProcessingStopping = true;
                    return TaskUtilities.CompletedTask;
                }

                if (_requestRejected)
                {
                    // 400 Bad Request
                    StatusCode = 400;
                    _keepAlive = false;
                }
                else
                {
                    // 500 Internal Server Error
                    StatusCode = 500;
                }

                ReasonPhrase = null;

                var responseHeaders = FrameResponseHeaders;
                responseHeaders.Reset();
                var dateHeaderValues = DateHeaderValueManager.GetDateHeaderValues();

                responseHeaders.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);
                responseHeaders.SetRawContentLength("0", _bytesContentLengthZero);

                if (ServerOptions.AddServerHeader)
                {
                    responseHeaders.SetRawServer(Constants.ServerName, _bytesServer);
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
            await SocketOutput.WriteAsync(_emptyData);

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
                ConnectionControl.End(ProduceEndType.ConnectionKeepAlive);
            }

            return TaskUtilities.CompletedTask;
        }

        private async Task WriteAutoChunkSuffixAwaited()
        {
            await WriteChunkedResponseSuffix();

            if (_keepAlive)
            {
                ConnectionControl.End(ProduceEndType.ConnectionKeepAlive);
            }
        }

        private void CreateResponseHeader(
            byte[] statusBytes,
            bool appCompleted)
        {
            var responseHeaders = FrameResponseHeaders;
            responseHeaders.SetReadOnly();

            var hasConnection = responseHeaders.HasConnection;

            var end = SocketOutput.ProducingStart();
            if (_keepAlive && hasConnection)
            {
                foreach (var connectionValue in responseHeaders.HeaderConnection)
                {
                    if (connectionValue.IndexOf("close", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        _keepAlive = false;
                        break;
                    }
                }
            }

            if (_keepAlive && !responseHeaders.HasTransferEncoding && !responseHeaders.HasContentLength)
            {
                if (appCompleted)
                {
                    // Don't set the Content-Length or Transfer-Encoding headers
                    // automatically for HEAD requests or 101, 204, 205, 304 responses.
                    if (Method != "HEAD" && StatusCanHaveBody(StatusCode))
                    {
                        // Since the app has completed and we are only now generating
                        // the headers we can safely set the Content-Length to 0.
                        responseHeaders.SetRawContentLength("0", _bytesContentLengthZero);
                    }
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
                    if (_httpVersion == HttpVersionType.Http11)
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

            if (!_keepAlive && !hasConnection && _httpVersion != HttpVersionType.Http10)
            {
                responseHeaders.SetRawConnection("close", _bytesConnectionClose);
            }
            else if (_keepAlive && !hasConnection && _httpVersion == HttpVersionType.Http10)
            {
                responseHeaders.SetRawConnection("keep-alive", _bytesConnectionKeepAlive);
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

            end.CopyFrom(_bytesHttpVersion11);
            end.CopyFrom(statusBytes);
            responseHeaders.CopyTo(ref end);
            end.CopyFrom(_bytesEndHeaders, 0, _bytesEndHeaders.Length);

            SocketOutput.ProducingComplete(end);
        }

        protected RequestLineStatus TakeStartLine(SocketInput input)
        {
            var scan = input.ConsumingStart();
            var consumed = scan;

            try
            {
                // We may hit this when the client has stopped sending data but
                // the connection hasn't closed yet, and therefore Frame.Stop()
                // hasn't been called yet.
                if (scan.Peek() == -1)
                {
                    return RequestLineStatus.Empty;
                }

                _requestProcessingStatus = RequestProcessingStatus.RequestStarted;

                string method;
                var begin = scan;
                if (!begin.GetKnownMethod(out method))
                {
                    if (scan.Seek(ref _vectorSpaces) == -1)
                    {
                        return RequestLineStatus.MethodIncomplete;
                    }

                    method = begin.GetAsciiString(scan);

                    if (method == null)
                    {
                        RejectRequest("Missing method.");
                    }

                    // Note: We're not in the fast path any more (GetKnownMethod should have handled any HTTP Method we're aware of)
                    // So we can be a tiny bit slower and more careful here.
                    for (int i = 0; i < method.Length; i++)
                    {
                        if (!IsValidTokenChar(method[i]))
                        {
                            RejectRequest("Invalid method.");
                        }
                    }
                }
                else
                {
                    scan.Skip(method.Length);
                }

                scan.Take();
                begin = scan;
                var needDecode = false;
                var chFound = scan.Seek(ref _vectorSpaces, ref _vectorQuestionMarks, ref _vectorPercentages);
                if (chFound == -1)
                {
                    return RequestLineStatus.TargetIncomplete;
                }
                else if (chFound == '%')
                {
                    needDecode = true;
                    chFound = scan.Seek(ref _vectorSpaces, ref _vectorQuestionMarks);
                    if (chFound == -1)
                    {
                        return RequestLineStatus.TargetIncomplete;
                    }
                }

                var pathBegin = begin;
                var pathEnd = scan;

                var queryString = "";
                if (chFound == '?')
                {
                    begin = scan;
                    if (scan.Seek(ref _vectorSpaces) == -1)
                    {
                        return RequestLineStatus.TargetIncomplete;
                    }
                    queryString = begin.GetAsciiString(scan);
                }

                var queryEnd = scan;

                if (pathBegin.Peek() == ' ')
                {
                    RejectRequest("Missing request target.");
                }

                scan.Take();
                begin = scan;
                if (scan.Seek(ref _vectorCRs) == -1)
                {
                    return RequestLineStatus.VersionIncomplete;
                }

                string httpVersion;
                if (!begin.GetKnownVersion(out httpVersion))
                {
                    // A slower fallback is necessary since the iterator's PeekLong() method
                    // used in GetKnownVersion() only examines two memory blocks at most.
                    // Although unlikely, it is possible that the 8 bytes forming the version
                    // could be spread out on more than two blocks, if the connection
                    // happens to be unusually slow.
                    httpVersion = begin.GetAsciiString(scan);

                    if (httpVersion == null)
                    {
                        RejectRequest("Missing HTTP version.");
                    }
                    else if (httpVersion != "HTTP/1.0" && httpVersion != "HTTP/1.1")
                    {
                        RejectRequest("Unrecognized HTTP version.");
                    }
                }

                scan.Take();
                var next = scan.Take();
                if (next == -1)
                {
                    return RequestLineStatus.Incomplete;
                }
                else if (next != '\n')
                {
                    RejectRequest("Missing LF in request line.");
                }

                // URIs are always encoded/escaped to ASCII https://tools.ietf.org/html/rfc3986#page-11
                // Multibyte Internationalized Resource Identifiers (IRIs) are first converted to utf8;
                // then encoded/escaped to ASCII  https://www.ietf.org/rfc/rfc3987.txt "Mapping of IRIs to URIs"
                string requestUrlPath;
                string rawTarget;
                if (needDecode)
                {
                    // Read raw target before mutating memory.
                    rawTarget = pathBegin.GetAsciiString(queryEnd);

                    // URI was encoded, unescape and then parse as utf8
                    pathEnd = UrlPathDecoder.Unescape(pathBegin, pathEnd);
                    requestUrlPath = pathBegin.GetUtf8String(pathEnd);
                }
                else
                {
                    // URI wasn't encoded, parse as ASCII
                    requestUrlPath = pathBegin.GetAsciiString(pathEnd);

                    if (queryString.Length == 0)
                    {
                        // No need to allocate an extra string if the path didn't need
                        // decoding and there's no query string following it.
                        rawTarget = requestUrlPath;
                    }
                    else
                    {
                        rawTarget = pathBegin.GetAsciiString(queryEnd);
                    }
                }

                var normalizedTarget = PathNormalizer.RemoveDotSegments(requestUrlPath);

                consumed = scan;
                Method = method;
                QueryString = queryString;
                RawTarget = rawTarget;
                HttpVersion = httpVersion;

                bool caseMatches;
                if (RequestUrlStartsWithPathBase(normalizedTarget, out caseMatches))
                {
                    PathBase = caseMatches ? _pathBase : normalizedTarget.Substring(0, _pathBase.Length);
                    Path = normalizedTarget.Substring(_pathBase.Length);
                }
                else if (rawTarget[0] == '/') // check rawTarget since normalizedTarget can be "" or "/" after dot segment removal
                {
                    Path = normalizedTarget;
                }
                else
                {
                    Path = string.Empty;
                    PathBase = string.Empty;
                    QueryString = string.Empty;
                }

                return RequestLineStatus.Done;
            }
            finally
            {
                input.ConsumingComplete(consumed, scan);
            }
        }

        private static bool IsValidTokenChar(char c)
        {
            // Determines if a character is valid as a 'token' as defined in the
            // HTTP spec: https://tools.ietf.org/html/rfc7230#section-3.2.6
            return
                (c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                c == '!' ||
                c == '#' ||
                c == '$' ||
                c == '%' ||
                c == '&' ||
                c == '\'' ||
                c == '*' ||
                c == '+' ||
                c == '-' ||
                c == '.' ||
                c == '^' ||
                c == '_' ||
                c == '`' ||
                c == '|' ||
                c == '~';
        }

        private bool RequestUrlStartsWithPathBase(string requestUrl, out bool caseMatches)
        {
            caseMatches = true;

            if (string.IsNullOrEmpty(_pathBase))
            {
                return false;
            }

            if (requestUrl.Length < _pathBase.Length || (requestUrl.Length > _pathBase.Length && requestUrl[_pathBase.Length] != '/'))
            {
                return false;
            }

            for (var i = 0; i < _pathBase.Length; i++)
            {
                if (requestUrl[i] != _pathBase[i])
                {
                    if (char.ToLowerInvariant(requestUrl[i]) == char.ToLowerInvariant(_pathBase[i]))
                    {
                        caseMatches = false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TakeMessageHeaders(SocketInput input, FrameRequestHeaders requestHeaders)
        {
            var scan = input.ConsumingStart();
            var consumed = scan;
            try
            {
                while (!scan.IsEnd)
                {
                    var ch = scan.Peek();
                    if (ch == -1)
                    {
                        return false;
                    }
                    else if (ch == '\r')
                    {
                        // Check for final CRLF.
                        scan.Take();
                        ch = scan.Take();

                        if (ch == -1)
                        {
                            return false;
                        }
                        else if (ch == '\n')
                        {
                            consumed = scan;
                            return true;
                        }

                        // Headers don't end in CRLF line.
                        RejectRequest("Headers corrupted, invalid header sequence.");
                    }
                    else if (ch == ' ' || ch == '\t')
                    {
                        RejectRequest("Header line must not start with whitespace.");
                    }

                    var beginName = scan;
                    if (scan.Seek(ref _vectorColons, ref _vectorCRs) == -1)
                    {
                        return false;
                    }
                    var endName = scan;

                    ch = scan.Take();
                    if (ch != ':')
                    {
                        RejectRequest("No ':' character found in header line.");
                    }

                    var validateName = beginName;
                    if (validateName.Seek(ref _vectorSpaces, ref _vectorTabs, ref _vectorColons) != ':')
                    {
                        RejectRequest("Whitespace is not allowed in header name.");
                    }

                    var beginValue = scan;
                    ch = scan.Peek();

                    if (ch == -1)
                    {
                        return false;
                    }

                    // Skip header value leading whitespace.
                    while (ch == ' ' || ch == '\t')
                    {
                        scan.Take();
                        beginValue = scan;

                        ch = scan.Peek();
                        if (ch == -1)
                        {
                            return false;
                        }
                    }

                    scan = beginValue;
                    if (scan.Seek(ref _vectorCRs) == -1)
                    {
                        // no "\r" in sight, burn used bytes and go back to await more data
                        return false;
                    }

                    scan.Take(); // we know this is '\r'
                    ch = scan.Take(); // expecting '\n'

                    if (ch == -1)
                    {
                        return false;
                    }
                    else if (ch != '\n')
                    {
                        RejectRequest("Header line must end in CRLF; only CR found.");
                    }

                    var next = scan.Peek();
                    if (next == -1)
                    {
                        return false;
                    }
                    else if (next == ' ' || next == '\t')
                    {
                        // From https://tools.ietf.org/html/rfc7230#section-3.2.4:
                        //
                        // Historically, HTTP header field values could be extended over
                        // multiple lines by preceding each extra line with at least one space
                        // or horizontal tab (obs-fold).  This specification deprecates such
                        // line folding except within the message/http media type
                        // (Section 8.3.1).  A sender MUST NOT generate a message that includes
                        // line folding (i.e., that has any field-value that contains a match to
                        // the obs-fold rule) unless the message is intended for packaging
                        // within the message/http media type.
                        //
                        // A server that receives an obs-fold in a request message that is not
                        // within a message/http container MUST either reject the message by
                        // sending a 400 (Bad Request), preferably with a representation
                        // explaining that obsolete line folding is unacceptable, or replace
                        // each received obs-fold with one or more SP octets prior to
                        // interpreting the field value or forwarding the message downstream.
                        RejectRequest("Header value line folding not supported.");
                    }

                    // Trim trailing whitespace from header value by repeatedly advancing to next
                    // whitespace or CR.
                    // 
                    // - If CR is found, this is the end of the header value.
                    // - If whitespace is found, this is the _tentative_ end of the header value.
                    //   If non-whitespace is found after it and it's not CR, seek again to the next
                    //   whitespace or CR for a new (possibly tentative) end of value.
                    var ws = beginValue;
                    var endValue = scan;
                    do
                    {
                        ws.Seek(ref _vectorSpaces, ref _vectorTabs, ref _vectorCRs);
                        endValue = ws;

                        ch = ws.Take();
                        while (ch == ' ' || ch == '\t')
                        {
                            ch = ws.Take();
                        }
                    } while (ch != '\r');

                    var name = beginName.GetArraySegment(endName);
                    var value = beginValue.GetAsciiString(endValue);

                    consumed = scan;
                    requestHeaders.Append(name.Array, name.Offset, name.Count, value);
                }

                return false;
            }
            finally
            {
                input.ConsumingComplete(consumed, scan);
            }
        }

        public bool StatusCanHaveBody(int statusCode)
        {
            // List of status codes taken from Microsoft.Net.Http.Server.Response
            return statusCode != 101 &&
                   statusCode != 204 &&
                   statusCode != 205 &&
                   statusCode != 304;
        }

        public void RejectRequest(string message)
        {
            var ex = new BadHttpRequestException(message);
            SetBadRequestState(ex);
            throw ex;
        }

        public void SetBadRequestState(BadHttpRequestException ex)
        {
            _requestProcessingStopping = true;
            _requestRejected = true;
            Log.ConnectionBadRequest(ConnectionId, ex);
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

            Log.ApplicationError(ConnectionId, ex);
        }

        private enum HttpVersionType
        {
            Unset = -1,
            Http10 = 0,
            Http11 = 1
        }

        protected enum RequestLineStatus
        {
            Empty,
            MethodIncomplete,
            TargetIncomplete,
            VersionIncomplete,
            Incomplete,
            Done
        }

        protected enum RequestProcessingStatus
        {
            RequestPending,
            RequestStarted,
            ResponseStarted
        }
    }
}
