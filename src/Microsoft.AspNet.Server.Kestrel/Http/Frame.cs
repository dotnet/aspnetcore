// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Primitives;
using System.Numerics;
using Microsoft.AspNet.Hosting.Builder;

// ReSharper disable AccessToModifiedClosure

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class Frame : FrameContext, IFrameControl
    {
        private static readonly Encoding _ascii = Encoding.ASCII;
        private static readonly ArraySegment<byte> _endChunkBytes = CreateAsciiByteArraySegment("\r\n");
        private static readonly ArraySegment<byte> _endChunkedResponseBytes = CreateAsciiByteArraySegment("0\r\n\r\n");
        private static readonly ArraySegment<byte> _continueBytes = CreateAsciiByteArraySegment("HTTP/1.1 100 Continue\r\n\r\n");
        private static readonly ArraySegment<byte> _emptyData = new ArraySegment<byte>(new byte[0]);
        private static readonly byte[] _hex = Encoding.ASCII.GetBytes("0123456789abcdef");

        private readonly object _onStartingSync = new Object();
        private readonly object _onCompletedSync = new Object();
        private readonly FrameRequestHeaders _requestHeaders = new FrameRequestHeaders();
        private readonly FrameResponseHeaders _responseHeaders = new FrameResponseHeaders();

        private List<KeyValuePair<Func<object, Task>, object>> _onStarting;
        private List<KeyValuePair<Func<object, Task>, object>> _onCompleted;

        private bool _responseStarted;
        private bool _keepAlive;
        private bool _autoChunk;

        public Frame(ConnectionContext context) : base(context)
        {
            FrameControl = this;
            Reset();
        }

        public string Method { get; set; }
        public string RequestUri { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string HttpVersion { get; set; }
        public IDictionary<string, StringValues> RequestHeaders { get; set; }
        public MessageBody MessageBody { get; set; }
        public Stream RequestBody { get; set; }

        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public IDictionary<string, StringValues> ResponseHeaders { get; set; }
        public Stream ResponseBody { get; set; }

        public Stream DuplexStream { get; set; }

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

            _requestHeaders.Reset();
            ResetResponseHeaders();

            Method = null;
            RequestUri = null;
            Path = null;
            QueryString = null;
            HttpVersion = null;
            RequestHeaders = _requestHeaders;
            MessageBody = null;
            RequestBody = null;
            StatusCode = 200;
            ReasonPhrase = null;
            ResponseHeaders = _responseHeaders;
            ResponseBody = null;
            DuplexStream = null;

        }

        public void ResetResponseHeaders()
        {
            _responseHeaders.Reset();
            _responseHeaders.HeaderServer = "Kestrel";
            _responseHeaders.HeaderDate = DateTime.UtcNow.ToString("r");
        }

        public async Task ProcessFraming()
        {
            var terminated = false;
            while (!terminated)
            {
                while (!terminated && !TakeStartLine(SocketInput))
                {
                    terminated = SocketInput.RemoteIntakeFin;
                    if (!terminated)
                    {
                        await SocketInput;
                    }
                }

                while (!terminated && !TakeMessageHeaders(SocketInput))
                {
                    terminated = SocketInput.RemoteIntakeFin;
                    if (!terminated)
                    {
                        await SocketInput;
                    }
                }

                if (!terminated)
                {
                    MessageBody = MessageBody.For(HttpVersion, _requestHeaders, this);
                    _keepAlive = MessageBody.RequestKeepAlive;
                    RequestBody = new FrameRequestStream(MessageBody);
                    ResponseBody = new FrameResponseStream(this);
                    DuplexStream = new FrameDuplexStream(RequestBody, ResponseBody);

                    Exception error = null;
                    try
                    {
                        await Application.Invoke(this).ConfigureAwait(false);

                        // Trigger FireOnStarting if ProduceStart hasn't been called yet.
                        // We call it here, so it can go through our normal error handling
                        // and respond with a 500 if an OnStarting callback throws.
                        if (!_responseStarted)
                        {
                            FireOnStarting();
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                    finally
                    {
                        FireOnCompleted();
                        ProduceEnd(error);
                    }

                    terminated = !_keepAlive;
                }

                Reset();
            }

            // Connection Terminated!
            ConnectionControl.End(ProduceEndType.SocketShutdownSend);

            // Wait for client to disconnect, or to receive unexpected data
            await SocketInput;

            ConnectionControl.End(ProduceEndType.SocketDisconnect);
        }

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

        private void FireOnStarting()
        {
            List<KeyValuePair<Func<object, Task>, object>> onStarting = null;
            lock (_onStartingSync)
            {
                onStarting = _onStarting;
                _onStarting = null;
            }
            if (onStarting != null)
            {
                foreach (var entry in onStarting)
                {
                    entry.Key.Invoke(entry.Value).Wait();
                }
            }
        }

        private void FireOnCompleted()
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
                        entry.Key.Invoke(entry.Value).Wait();
                    }
                    catch
                    {
                        // Ignore exceptions
                    }
                }
            }
        }

        public void Flush()
        {
            ProduceStart(immediate: false);
            SocketOutput.Write(_emptyData, immediate: true);
        }

        public Task FlushAsync(CancellationToken cancellationToken)
        {
            ProduceStart(immediate: false);
            return SocketOutput.WriteAsync(_emptyData, immediate: true);
        }

        public void Write(ArraySegment<byte> data)
        {
            ProduceStart(immediate: false);

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
            ProduceStart(immediate: false);

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

        private void WriteChunked(ArraySegment<byte> data)
        {
            SocketOutput.Write(BeginChunkBytes(data.Count), immediate: false);
            SocketOutput.Write(data, immediate: false);
            SocketOutput.Write(_endChunkBytes, immediate: true);
        }

        private async Task WriteChunkedAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
        {
            await SocketOutput.WriteAsync(BeginChunkBytes(data.Count), immediate: false, cancellationToken: cancellationToken);
            await SocketOutput.WriteAsync(data, immediate: false, cancellationToken: cancellationToken);
            await SocketOutput.WriteAsync(_endChunkBytes, immediate: true, cancellationToken: cancellationToken);
        }

        public static ArraySegment<byte> BeginChunkBytes(int dataCount)
        {
            var bytes = new byte[10]
            {
                _hex[((dataCount >> 0x1c) & 0x0f)],
                _hex[((dataCount >> 0x18) & 0x0f)],
                _hex[((dataCount >> 0x14) & 0x0f)],
                _hex[((dataCount >> 0x10) & 0x0f)],
                _hex[((dataCount >> 0x0c) & 0x0f)],
                _hex[((dataCount >> 0x08) & 0x0f)],
                _hex[((dataCount >> 0x04) & 0x0f)],
                _hex[((dataCount >> 0x00) & 0x0f)],
                (byte)'\r',
                (byte)'\n',
            };

            // Determine the most-significant non-zero nibble
            int total, shift;
            total = (dataCount > 0xffff) ? 0x10 : 0x00;
            dataCount >>= total;
            shift = (dataCount > 0x00ff) ? 0x08 : 0x00;
            dataCount >>= shift;
            total |= shift;
            total |= (dataCount > 0x000f) ? 0x04 : 0x00;

            var offset = 7 - (total >> 2);
            return new ArraySegment<byte>(bytes, offset, 10 - offset);
        }

        private void WriteChunkedResponseSuffix()
        {
            SocketOutput.Write(_endChunkedResponseBytes, immediate: true);
        }

        public void Upgrade(IDictionary<string, object> options, Func<object, Task> callback)
        {
            _keepAlive = false;
            ProduceStart();
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
            if (HttpVersion.Equals("HTTP/1.1") &&
                RequestHeaders.TryGetValue("Expect", out expect) &&
                (expect.FirstOrDefault() ?? "").Equals("100-continue", StringComparison.OrdinalIgnoreCase))
            {
                SocketOutput.Write(_continueBytes);
            }
        }

        public void ProduceStart(bool immediate = true, bool appCompleted = false)
        {
            // ProduceStart shouldn't no-op in the future just b/c FireOnStarting throws.
            if (_responseStarted) return;
            FireOnStarting();
            _responseStarted = true;

            var status = ReasonPhrases.ToStatus(StatusCode, ReasonPhrase);

            var responseHeader = CreateResponseHeader(status, appCompleted, ResponseHeaders);
            SocketOutput.Write(responseHeader.Item1, immediate: immediate);
            responseHeader.Item2.Dispose();
        }

        public void ProduceEnd(Exception ex)
        {
            if (ex != null)
            {
                if (_responseStarted)
                {
                    // We can no longer respond with a 500, so we simply close the connection.
                    ConnectionControl.End(ProduceEndType.SocketDisconnect);
                    return;
                }
                else
                {
                    StatusCode = 500;
                    ReasonPhrase = null;

                    // If OnStarting hasn't been triggered yet, we don't want to trigger it now that
                    // the app func has failed. https://github.com/aspnet/KestrelHttpServer/issues/43
                    _onStarting = null;

                    ResetResponseHeaders();
                    _responseHeaders.HeaderContentLength = "0";
                }
            }

            ProduceStart(immediate: true, appCompleted: true);

            // _autoChunk should be checked after we are sure ProduceStart() has been called
            // since ProduceStart() may set _autoChunk to true.
            if (_autoChunk)
            {
                WriteChunkedResponseSuffix();
            }

            ConnectionControl.End(_keepAlive ? ProduceEndType.ConnectionKeepAlive : ProduceEndType.SocketShutdownSend);
        }

        private Tuple<ArraySegment<byte>, IDisposable> CreateResponseHeader(
            string status,
            bool appCompleted,
            IEnumerable<KeyValuePair<string, StringValues>> headers)
        {
            var writer = new MemoryPoolTextWriter(Memory);
            writer.Write(HttpVersion);
            writer.Write(' ');
            writer.Write(status);
            writer.Write('\r');
            writer.Write('\n');

            var hasConnection = false;
            var hasTransferEncoding = false;
            var hasContentLength = false;
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    var isConnection = false;
                    if (!hasConnection &&
                        string.Equals(header.Key, "Connection", StringComparison.OrdinalIgnoreCase))
                    {
                        hasConnection = isConnection = true;
                    }
                    else if (!hasTransferEncoding &&
                        string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                    {
                        hasTransferEncoding = true;
                    }
                    else if (!hasContentLength &&
                        string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        hasContentLength = true;
                    }

                    foreach (var value in header.Value)
                    {
                        writer.Write(header.Key);
                        writer.Write(':');
                        writer.Write(' ');
                        writer.Write(value);
                        writer.Write('\r');
                        writer.Write('\n');

                        if (isConnection && value.IndexOf("close", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            _keepAlive = false;
                        }
                    }
                }
            }

            if (_keepAlive && !hasTransferEncoding && !hasContentLength)
            {
                if (appCompleted)
                {
                    // Don't set the Content-Length or Transfer-Encoding headers
                    // automatically for HEAD requests or 101, 204, 205, 304 responses.
                    if (Method != "HEAD" && StatusCanHaveBody(StatusCode))
                    {
                        // Since the app has completed and we are only now generating
                        // the headers we can safely set the Content-Length to 0.
                        writer.Write("Content-Length: 0\r\n");
                    }
                }
                else
                {
                    if (HttpVersion == "HTTP/1.1")
                    {
                        _autoChunk = true;
                        writer.Write("Transfer-Encoding: chunked\r\n");
                    }
                    else
                    {
                        _keepAlive = false;
                    }
                }
            }

            if (_keepAlive == false && hasConnection == false && HttpVersion == "HTTP/1.1")
            {
                writer.Write("Connection: close\r\n\r\n");
            }
            else if (_keepAlive && hasConnection == false && HttpVersion == "HTTP/1.0")
            {
                writer.Write("Connection: keep-alive\r\n\r\n");
            }
            else
            {
                writer.Write('\r');
                writer.Write('\n');
            }
            writer.Flush();
            return new Tuple<ArraySegment<byte>, IDisposable>(writer.Buffer, writer);
        }

        private bool TakeStartLine(SocketInput input)
        {
            var scan = input.GetIterator();

            var begin = scan;
            if (scan.Seek(' ') == -1)
            {
                return false;
            }
            var method = begin.GetString(scan);

            scan.Take();
            begin = scan;
            var chFound = scan.Seek(' ', '?');
            if (chFound == -1)
            {
                return false;
            }
            var requestUri = begin.GetString(scan);

            var queryString = "";
            if (chFound == '?')
            {
                begin = scan;
                if (scan.Seek(' ') != ' ')
                {
                    return false;
                }
                queryString = begin.GetString(scan);
            }

            scan.Take();
            begin = scan;
            if (scan.Seek('\r') == -1)
            {
                return false;
            }
            var httpVersion = begin.GetString(scan);

            scan.Take();
            if (scan.Take() != '\n') return false;

            Method = method;
            RequestUri = requestUri;
            QueryString = queryString;
            HttpVersion = httpVersion;
            input.JumpTo(scan);
            return true;
        }

        static string GetString(ArraySegment<byte> range, int startIndex, int endIndex)
        {
            return Encoding.UTF8.GetString(range.Array, range.Offset + startIndex, endIndex - startIndex);
        }

        private bool TakeMessageHeaders(SocketInput input)
        {
            int chFirst;
            int chSecond;
            var scan = input.GetIterator();
            var consumed = scan;
            try
            {
                while (!scan.IsEnd)
                {
                    var beginName = scan;
                    scan.Seek(':', '\r');
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
                        throw new Exception("Malformed request");
                    }

                    while (
                        chSecond == ' ' ||
                        chSecond == '\t' ||
                        chSecond == '\r' ||
                        chSecond == '\n')
                    {
                        beginValue = scan;
                        chSecond = scan.Take();
                    }
                    scan = beginValue;

                    var wrapping = false;
                    while (!scan.IsEnd)
                    {
                        if (scan.Seek('\r') == -1)
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
#if DEBUG
                        var nameString = beginName.GetString(endName);
#endif
                        var value = beginValue.GetString(endValue);
                        if (wrapping)
                        {
                            value = value.Replace("\r\n", " ");
                        }

                        _requestHeaders.Append(name.Array, name.Offset, name.Count, value);
                        consumed = scan;
                        break;
                    }
                }
                return false;
            }
            finally
            {
                input.JumpTo(consumed);
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
    }
}
