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
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

// ReSharper disable AccessToModifiedClosure

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public abstract partial class Frame : FrameContext, IFrameControl
    {
        private static readonly Encoding _ascii = Encoding.ASCII;
        private static readonly ArraySegment<byte> _endChunkedResponseBytes = CreateAsciiByteArraySegment("0\r\n\r\n");
        private static readonly ArraySegment<byte> _continueBytes = CreateAsciiByteArraySegment("HTTP/1.1 100 Continue\r\n\r\n");
        private static readonly ArraySegment<byte> _emptyData = new ArraySegment<byte>(new byte[0]);

        private static readonly byte[] _bytesConnectionClose = Encoding.ASCII.GetBytes("\r\nConnection: close");
        private static readonly byte[] _bytesConnectionKeepAlive = Encoding.ASCII.GetBytes("\r\nConnection: keep-alive");
        private static readonly byte[] _bytesTransferEncodingChunked = Encoding.ASCII.GetBytes("\r\nTransfer-Encoding: chunked");
        private static readonly byte[] _bytesHttpVersion1_0 = Encoding.ASCII.GetBytes("HTTP/1.0 ");
        private static readonly byte[] _bytesHttpVersion1_1 = Encoding.ASCII.GetBytes("HTTP/1.1 ");
        private static readonly byte[] _bytesContentLengthZero = Encoding.ASCII.GetBytes("\r\nContent-Length: 0");
        private static readonly byte[] _bytesSpace = Encoding.ASCII.GetBytes(" ");
        private static readonly byte[] _bytesServer = Encoding.ASCII.GetBytes("\r\nServer: Kestrel");
        private static readonly byte[] _bytesDate = Encoding.ASCII.GetBytes("Date: ");
        private static readonly byte[] _bytesEndHeaders = Encoding.ASCII.GetBytes("\r\n\r\n");

        private static Vector<byte> _vectorCRs = new Vector<byte>((byte)'\r');
        private static Vector<byte> _vectorColons = new Vector<byte>((byte)':');
        private static Vector<byte> _vectorSpaces = new Vector<byte>((byte)' ');
        private static Vector<byte> _vectorQuestionMarks = new Vector<byte>((byte)'?');
        private static Vector<byte> _vectorPercentages = new Vector<byte>((byte)'%');

        private readonly object _onStartingSync = new Object();
        private readonly object _onCompletedSync = new Object();
        protected readonly FrameRequestHeaders _requestHeaders = new FrameRequestHeaders();
        private readonly FrameResponseHeaders _responseHeaders = new FrameResponseHeaders();

        protected List<KeyValuePair<Func<object, Task>, object>> _onStarting;

        protected List<KeyValuePair<Func<object, Task>, object>> _onCompleted;

        private bool _requestProcessingStarted;
        private Task _requestProcessingTask;
        protected volatile bool _requestProcessingStopping; // volatile, see: https://msdn.microsoft.com/en-us/library/x13ttww7.aspx
        protected volatile bool _requestAborted;
        protected CancellationTokenSource _abortedCts;
        protected CancellationToken? _manuallySetRequestAbortToken;

        internal FrameRequestStream _requestBody;
        internal FrameResponseStream _responseBody;
        internal FrameDuplexStream _duplexStream;

        protected bool _responseStarted;
        protected bool _keepAlive;
        private bool _autoChunk;
        protected Exception _applicationException;

        private HttpVersionType _httpVersion;

        private readonly IPEndPoint _localEndPoint;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly Action<IFeatureCollection> _prepareRequest;

        private readonly string _pathBase;

        public Frame(ConnectionContext context)
            : this(context, remoteEndPoint: null, localEndPoint: null, prepareRequest: null)
        {
        }

        public Frame(ConnectionContext context,
                     IPEndPoint remoteEndPoint,
                     IPEndPoint localEndPoint,
                     Action<IFeatureCollection> prepareRequest)
            : base(context)
        {
            _remoteEndPoint = remoteEndPoint;
            _localEndPoint = localEndPoint;
            _prepareRequest = prepareRequest;
            _pathBase = context.ServerAddress.PathBase;
            if (ReuseStreams)
            {
                _requestBody = new FrameRequestStream();
                _responseBody = new FrameResponseStream(this);
                _duplexStream = new FrameDuplexStream(_requestBody, _responseBody);
            }

            FrameControl = this;
            Reset();
        }

        public string Scheme { get; set; }
        public string Method { get; set; }
        public string RequestUri { get; set; }
        public string PathBase { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string HttpVersion
        {
            get
            {
                if (_httpVersion == HttpVersionType.Http1_1)
                {
                    return "HTTP/1.1";
                }
                if (_httpVersion == HttpVersionType.Http1_0)
                {
                    return "HTTP/1.0";
                }
                return "";
            }
            set
            {
                if (value == "HTTP/1.1")
                {
                    _httpVersion = HttpVersionType.Http1_1;
                }
                else if (value == "HTTP/1.0")
                {
                    _httpVersion = HttpVersionType.Http1_0;
                }
                else
                {
                    _httpVersion = HttpVersionType.Unknown;
                }
            }
        }

        public IHeaderDictionary RequestHeaders { get; set; }
        public Stream RequestBody { get; set; }

        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
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
                    _requestAborted ? new CancellationToken(true) :
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
                var cts = LazyInitializer.EnsureInitialized(ref _abortedCts, () => new CancellationTokenSource());
                if (_requestAborted)
                {
                    cts.Cancel();
                }
                return cts;
            }
        }
        public bool HasResponseStarted
        {
            get { return _responseStarted; }
        }

        public void Reset()
        {
            _onStarting = null;
            _onCompleted = null;

            _responseStarted = false;
            _keepAlive = false;
            _autoChunk = false;
            _applicationException = null;

            _requestHeaders.Reset();
            ResetResponseHeaders();
            ResetFeatureCollection();

            Scheme = null;
            Method = null;
            RequestUri = null;
            PathBase = null;
            Path = null;
            QueryString = null;
            _httpVersion = HttpVersionType.Unknown;
            RequestHeaders = _requestHeaders;
            RequestBody = null;
            StatusCode = 200;
            ReasonPhrase = null;
            ResponseHeaders = _responseHeaders;
            ResponseBody = null;
            DuplexStream = null;

            var httpConnectionFeature = this as IHttpConnectionFeature;
            httpConnectionFeature.RemoteIpAddress = _remoteEndPoint?.Address;
            httpConnectionFeature.RemotePort = _remoteEndPoint?.Port ?? 0;

            httpConnectionFeature.LocalIpAddress = _localEndPoint?.Address;
            httpConnectionFeature.LocalPort = _localEndPoint?.Port ?? 0;

            if (_remoteEndPoint != null && _localEndPoint != null)
            {
                httpConnectionFeature.IsLocal = _remoteEndPoint.Address.Equals(_localEndPoint.Address);
            }
            else
            {
                httpConnectionFeature.IsLocal = false;
            }

            _prepareRequest?.Invoke(this);

            _manuallySetRequestAbortToken = null;
            _abortedCts = null;
        }

        public void ResetResponseHeaders()
        {
            _responseHeaders.Reset();
            _responseHeaders.SetRawDate(
                DateHeaderValueManager.GetDateHeaderValue(),
                DateHeaderValueManager.GetDateHeaderValueBytes());
            _responseHeaders.SetRawServer(
                "Kestrel",
                _bytesServer);
        }

        /// <summary>
        /// Called once by Connection class to begin the RequestProcessingAsync loop.
        /// </summary>
        public void Start()
        {
            if (!_requestProcessingStarted)
            {
                _requestProcessingStarted = true;
                _requestProcessingTask =
                    Task.Factory.StartNew(
                        (o) => ((Frame)o).RequestProcessingAsync(),
                        this,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default);
            }
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
        public void Abort()
        {
            _requestProcessingStopping = true;
            _requestAborted = true;

            _requestBody?.Abort();
            _responseBody?.Abort();

            try
            {
                ConnectionControl.End(ProduceEndType.SocketDisconnect);
                SocketInput.AbortAwaiting();
                RequestAbortedSource.Cancel();
            }
            catch (Exception ex)
            {
                Log.LogError("Abort", ex);
            }
            finally
            {
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
            ProduceStartAndFireOnStarting(immediate: false).GetAwaiter().GetResult();
            SocketOutput.Write(_emptyData, immediate: true);
        }

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            await ProduceStartAndFireOnStarting(immediate: false);
            await SocketOutput.WriteAsync(_emptyData, immediate: true, cancellationToken: cancellationToken);
        }

        public void Write(ArraySegment<byte> data)
        {
            ProduceStartAndFireOnStarting(immediate: false).GetAwaiter().GetResult();

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
                SocketOutput.Write(data, immediate: true);
            }
        }

        public Task WriteAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            if (!_responseStarted)
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
                return SocketOutput.WriteAsync(data, immediate: true, cancellationToken: cancellationToken);
            }
        }

        public async Task WriteAsyncAwaited(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            await ProduceStartAndFireOnStarting(immediate: false);

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
                await SocketOutput.WriteAsync(data, immediate: true, cancellationToken: cancellationToken);
            }
        }

        private void WriteChunked(ArraySegment<byte> data)
        {
            SocketOutput.Write(data, immediate: false, chunk: true);
        }

        private async Task WriteChunkedAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            await SocketOutput.WriteAsync(data, immediate: false, chunk: true, cancellationToken: cancellationToken);
        }

        private void WriteChunkedResponseSuffix()
        {
            SocketOutput.Write(_endChunkedResponseBytes, immediate: true);
        }

        private static ArraySegment<byte> CreateAsciiByteArraySegment(string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            return new ArraySegment<byte>(bytes);
        }

        public void ProduceContinue()
        {
            if (_responseStarted) return;

            StringValues expect;
            if (_httpVersion == HttpVersionType.Http1_1 &&
                RequestHeaders.TryGetValue("Expect", out expect) &&
                (expect.FirstOrDefault() ?? "").Equals("100-continue", StringComparison.OrdinalIgnoreCase))
            {
                SocketOutput.Write(_continueBytes);
            }
        }

        public Task ProduceStartAndFireOnStarting(bool immediate = true)
        {
            if (_responseStarted) return TaskUtilities.CompletedTask;

            if (_onStarting != null)
            {
                return FireOnStartingProduceStart(immediate: immediate);
            }

            if (_applicationException != null)
            {
                throw new ObjectDisposedException(
                    "The response has been aborted due to an unhandled application exception.",
                    _applicationException);
            }

            return ProduceStart(immediate, appCompleted: false);
        }

        private async Task FireOnStartingProduceStart(bool immediate)
        {
            await FireOnStarting();

            if (_applicationException != null)
            {
                throw new ObjectDisposedException(
                    "The response has been aborted due to an unhandled application exception.",
                    _applicationException);
            }

            await ProduceStart(immediate, appCompleted: false);
        }

        private Task ProduceStart(bool immediate, bool appCompleted)
        {
            if (_responseStarted) return TaskUtilities.CompletedTask;
            _responseStarted = true;

            var statusBytes = ReasonPhrases.ToStatusBytes(StatusCode, ReasonPhrase);

            return CreateResponseHeader(statusBytes, appCompleted, immediate);
        }

        protected Task ProduceEnd()
        {
            if (_applicationException != null)
            {
                if (_responseStarted)
                {
                    // We can no longer respond with a 500, so we simply close the connection.
                    _requestProcessingStopping = true;
                    return TaskUtilities.CompletedTask;
                }
                else
                {
                    StatusCode = 500;
                    ReasonPhrase = null;

                    ResetResponseHeaders();
                    _responseHeaders.SetRawContentLength("0", _bytesContentLengthZero);
                }
            }


            if (!_responseStarted)
            {
                return ProduceEndAwaited();
            }

            WriteSuffix();

            return TaskUtilities.CompletedTask;
        }

        private async Task ProduceEndAwaited()
        {
            await ProduceStart(immediate: true, appCompleted: true);

            WriteSuffix();
        }

        private void WriteSuffix()
        {
            // _autoChunk should be checked after we are sure ProduceStart() has been called
            // since ProduceStart() may set _autoChunk to true.
            if (_autoChunk)
            {
                WriteChunkedResponseSuffix();
            }

            if (_keepAlive)
            {
                ConnectionControl.End(ProduceEndType.ConnectionKeepAlive);
            }
        }

        private Task CreateResponseHeader(
            byte[] statusBytes,
            bool appCompleted,
            bool immediate)
        {
            var begin = SocketOutput.ProducingStart();
            var end = begin;
            if (_keepAlive)
            {
                foreach (var connectionValue in _responseHeaders.HeaderConnection)
                {
                    if (connectionValue.IndexOf("close", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        _keepAlive = false;
                    }
                }
            }

            if (_keepAlive && !_responseHeaders.HasTransferEncoding && !_responseHeaders.HasContentLength)
            {
                if (appCompleted)
                {
                    // Don't set the Content-Length or Transfer-Encoding headers
                    // automatically for HEAD requests or 101, 204, 205, 304 responses.
                    if (Method != "HEAD" && StatusCanHaveBody(StatusCode))
                    {
                        // Since the app has completed and we are only now generating
                        // the headers we can safely set the Content-Length to 0.
                        _responseHeaders.SetRawContentLength("0", _bytesContentLengthZero);
                    }
                }
                else
                {
                    if (_httpVersion == HttpVersionType.Http1_1)
                    {
                        _autoChunk = true;
                        _responseHeaders.SetRawTransferEncoding("chunked", _bytesTransferEncodingChunked);
                    }
                    else
                    {
                        _keepAlive = false;
                    }
                }
            }

            if (_keepAlive == false && _responseHeaders.HasConnection == false && _httpVersion == HttpVersionType.Http1_1)
            {
                _responseHeaders.SetRawConnection("close", _bytesConnectionClose);
            }
            else if (_keepAlive && _responseHeaders.HasConnection == false && _httpVersion == HttpVersionType.Http1_0)
            {
                _responseHeaders.SetRawConnection("keep-alive", _bytesConnectionKeepAlive);
            }

            end.CopyFrom(_httpVersion == HttpVersionType.Http1_1 ? _bytesHttpVersion1_1 : _bytesHttpVersion1_0);
            end.CopyFrom(statusBytes);
            _responseHeaders.CopyTo(ref end);
            end.CopyFrom(_bytesEndHeaders, 0, _bytesEndHeaders.Length);

            SocketOutput.ProducingComplete(end);

            if (immediate)
            {
                return SocketOutput.WriteAsync(default(ArraySegment<byte>), immediate: true);
            }
            else
            {
                return TaskUtilities.CompletedTask;
            }
        }

        protected bool TakeStartLine(SocketInput input)
        {
            var scan = input.ConsumingStart();
            var consumed = scan;
            try
            {
                string method;
                var begin = scan;
                if (!begin.GetKnownMethod(ref scan,out method))
                {
                    if (scan.Seek(ref _vectorSpaces) == -1)
                    {
                        return false;
                    }
                    method = begin.GetAsciiString(scan);
                    scan.Take();
                }

                begin = scan;

                var needDecode = false;
                var chFound = scan.Seek(ref _vectorSpaces, ref _vectorQuestionMarks, ref _vectorPercentages);
                if (chFound == '%')
                {
                    needDecode = true;
                    chFound = scan.Seek(ref _vectorSpaces, ref _vectorQuestionMarks);
                }

                var pathBegin = begin;
                var pathEnd = scan;

                var queryString = "";
                if (chFound == '?')
                {
                    begin = scan;
                    if (scan.Seek(ref _vectorSpaces) != ' ')
                    {
                        return false;
                    }
                    queryString = begin.GetAsciiString(scan);
                }

                scan.Take();
                begin = scan;

                string httpVersion;
                if (!begin.GetKnownVersion(ref scan, out httpVersion))
                {
                    scan = begin;
                    if (scan.Seek(ref _vectorCRs) == -1)
                    {
                        return false;
                    }
                    httpVersion = begin.GetAsciiString(scan);

                    scan.Take();
                }
                if (scan.Take() != '\n')
                {
                    return false;
                }

                // URIs are always encoded/escaped to ASCII https://tools.ietf.org/html/rfc3986#page-11 
                // Multibyte Internationalized Resource Identifiers (IRIs) are first converted to utf8; 
                // then encoded/escaped to ASCII  https://www.ietf.org/rfc/rfc3987.txt "Mapping of IRIs to URIs"
                string requestUrlPath;
                if (needDecode)
                {
                    // URI was encoded, unescape and then parse as utf8
                    pathEnd = UrlPathDecoder.Unescape(pathBegin, pathEnd);
                    requestUrlPath = pathBegin.GetUtf8String(pathEnd);
                }
                else
                {
                    // URI wasn't encoded, parse as ASCII
                    requestUrlPath = pathBegin.GetAsciiString(pathEnd);
                }

                consumed = scan;
                Method = method;
                RequestUri = requestUrlPath;
                QueryString = queryString;
                HttpVersion = httpVersion;

                bool caseMatches;

                if (!string.IsNullOrEmpty(_pathBase) &&
                    (requestUrlPath.Length == _pathBase.Length || (requestUrlPath.Length > _pathBase.Length && requestUrlPath[_pathBase.Length] == '/')) &&
                    RequestUrlStartsWithPathBase(requestUrlPath, out caseMatches))
                {
                    PathBase = caseMatches ? _pathBase : requestUrlPath.Substring(0, _pathBase.Length);
                    Path = requestUrlPath.Substring(_pathBase.Length);
                }
                else
                {
                    Path = requestUrlPath;
                }

                return true;
            }
            finally
            {
                input.ConsumingComplete(consumed, scan);
            }
        }

        private bool RequestUrlStartsWithPathBase(string requestUrl, out bool caseMatches)
        {
            caseMatches = true;

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

        public static bool TakeMessageHeaders(SocketInput input, FrameRequestHeaders requestHeaders)
        {
            var scan = input.ConsumingStart();
            var consumed = scan;
            try
            {
                int chFirst;
                int chSecond;
                while (!scan.IsEnd)
                {
                    var beginName = scan;
                    scan.Seek(ref _vectorColons, ref _vectorCRs);
                    var endName = scan;

                    chFirst = scan.Take();
                    var beginValue = scan;
                    chSecond = scan.Take();

                    if (chFirst == -1 || chSecond == -1)
                    {
                        return false;
                    }
                    if (chFirst == '\r')
                    {
                        if (chSecond == '\n')
                        {
                            consumed = scan;
                            return true;
                        }
                        throw new InvalidDataException("Malformed request");
                    }

                    while (
                        chSecond == ' ' ||
                        chSecond == '\t' ||
                        chSecond == '\r' ||
                        chSecond == '\n')
                    {
                        if (chSecond == '\r')
                        {
                            var scanAhead = scan;
                            var chAhead = scanAhead.Take();
                            if (chAhead == '\n')
                            {
                                chAhead = scanAhead.Take();
                                // If the "\r\n" isn't part of "linear whitespace",
                                // then this header has no value.
                                if (chAhead != ' ' && chAhead != '\t')
                                {
                                    break;
                                }
                            }
                        }

                        beginValue = scan;
                        chSecond = scan.Take();
                    }
                    scan = beginValue;

                    var wrapping = false;
                    while (!scan.IsEnd)
                    {
                        if (scan.Seek(ref _vectorCRs) == -1)
                        {
                            // no "\r" in sight, burn used bytes and go back to await more data
                            return false;
                        }

                        var endValue = scan;
                        chFirst = scan.Take(); // expecting: /r
                        chSecond = scan.Take(); // expecting: /n

                        if (chSecond != '\n')
                        {
                            // "\r" was all by itself, move just after it and try again
                            scan = endValue;
                            scan.Take();
                            continue;
                        }

                        var chThird = scan.Peek();
                        if (chThird == ' ' || chThird == '\t')
                        {
                            // special case, "\r\n " or "\r\n\t".
                            // this is considered wrapping"linear whitespace" and is actually part of the header value
                            // continue past this for the next
                            wrapping = true;
                            continue;
                        }

                        var name = beginName.GetArraySegment(endName);
                        var value = beginValue.GetAsciiString(endValue);
                        if (wrapping)
                        {
                            value = value.Replace("\r\n", " ");
                        }

                        consumed = scan;
                        requestHeaders.Append(name.Array, name.Offset, name.Count, value);
                        break;
                    }
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

        protected void ReportApplicationError(Exception ex)
        {
            if (_applicationException == null)
            {
                _applicationException = ex;
            }
            else
            {
                _applicationException = new AggregateException(_applicationException, ex);
            }
            Log.ApplicationError(ex);
        }

        private enum HttpVersionType
        {
            Unknown = -1,
            Http1_0 = 0,
            Http1_1 = 1
        }
    }
}
