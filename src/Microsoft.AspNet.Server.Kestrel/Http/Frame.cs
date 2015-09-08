// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Primitives;

// ReSharper disable AccessToModifiedClosure

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public class Frame : FrameContext, IFrameControl
    {
        private static Encoding _ascii = Encoding.ASCII;
        private static readonly ArraySegment<byte> _endChunkBytes = CreateAsciiByteArraySegment("\r\n");
        private static readonly ArraySegment<byte> _endChunkedResponseBytes = CreateAsciiByteArraySegment("0\r\n\r\n");
        private static readonly ArraySegment<byte> _continueBytes = CreateAsciiByteArraySegment("HTTP/1.1 100 Continue\r\n\r\n");

        private Mode _mode;
        private bool _responseStarted;
        private bool _keepAlive;
        private bool _autoChunk;
        private readonly FrameRequestHeaders _requestHeaders = new FrameRequestHeaders();
        private readonly FrameResponseHeaders _responseHeaders = new FrameResponseHeaders();

        private List<KeyValuePair<Func<object, Task>, object>> _onStarting;
        private List<KeyValuePair<Func<object, Task>, object>> _onCompleted;
        private object _onStartingSync = new Object();
        private object _onCompletedSync = new Object();

        public Frame(ConnectionContext context) : base(context)
        {
            FrameControl = this;
            StatusCode = 200;
            RequestHeaders = _requestHeaders;
            ResponseHeaders = _responseHeaders;
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

        public void Consume()
        {
            var input = SocketInput;
            for (; ;)
            {
                switch (_mode)
                {
                    case Mode.StartLine:
                        if (input.Buffer.Count == 0 && input.RemoteIntakeFin)
                        {
                            _mode = Mode.Terminated;
                            break;
                        }

                        if (!TakeStartLine(input))
                        {
                            if (input.RemoteIntakeFin)
                            {
                                _mode = Mode.Terminated;
                                break;
                            }
                            return;
                        }

                        _mode = Mode.MessageHeader;
                        break;

                    case Mode.MessageHeader:
                        if (input.Buffer.Count == 0 && input.RemoteIntakeFin)
                        {
                            _mode = Mode.Terminated;
                            break;
                        }

                        var endOfHeaders = false;
                        while (!endOfHeaders)
                        {
                            if (!TakeMessageHeader(input, out endOfHeaders))
                            {
                                if (input.RemoteIntakeFin)
                                {
                                    _mode = Mode.Terminated;
                                    break;
                                }
                                return;
                            }
                        }

                        if (_mode == Mode.Terminated)
                        {
                            // If we broke out of the above while loop in the Terminated
                            // state, we don't want to transition to the MessageBody state.
                            break;
                        }

                        _mode = Mode.MessageBody;
                        Execute();
                        break;

                    case Mode.MessageBody:
                        if (MessageBody.LocalIntakeFin)
                        {
                            // NOTE: stop reading and resume on keepalive?
                            return;
                        }
                        MessageBody.Consume();
                        // NOTE: keep looping?
                        return;

                    case Mode.Terminated:
                        ConnectionControl.End(ProduceEndType.SocketShutdownSend);
                        ConnectionControl.End(ProduceEndType.SocketDisconnect);
                        return;
                }
            }
        }

        private void Execute()
        {
            MessageBody = MessageBody.For(
                HttpVersion,
                RequestHeaders,
                this);
            _keepAlive = MessageBody.RequestKeepAlive;
            RequestBody = new FrameRequestStream(MessageBody);
            ResponseBody = new FrameResponseStream(this);
            DuplexStream = new FrameDuplexStream(RequestBody, ResponseBody);
            SocketInput.Free();
            Task.Run(ExecuteAsync);
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

        private async Task ExecuteAsync()
        {
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
        }

        public void Write(ArraySegment<byte> data, Action<Exception, object> callback, object state)
        {
            ProduceStart(immediate: false);

            if (_autoChunk)
            {
                if (data.Count == 0)
                {
                    callback(null, state);
                    return;
                }

                WriteChunkPrefix(data.Count);
            }

            SocketOutput.Write(data, callback, state, immediate: !_autoChunk);

            if (_autoChunk)
            {
                WriteChunkSuffix();
            }
        }

        private void WriteChunkPrefix(int numOctets)
        {
            var numOctetBytes = CreateAsciiByteArraySegment(numOctets.ToString("x") + "\r\n");

            SocketOutput.Write(numOctetBytes,
                    (error, _) =>
                    {
                        if (error != null)
                        {
                            Log.LogError("WriteChunkPrefix", error);
                        }
                    },
                    null,
                    immediate: false);
        }

        private void WriteChunkSuffix()
        {
            SocketOutput.Write(_endChunkBytes,
                (error, _) =>
                {
                    if (error != null)
                    {
                        Log.LogError("WriteChunkSuffix", error);
                    }
                },
                null,
                immediate: true);
        }

        private void WriteChunkedResponseSuffix()
        {
            SocketOutput.Write(_endChunkedResponseBytes,
                (error, _) =>
                {
                    if (error != null)
                    {
                        Log.LogError("WriteChunkedResponseSuffix", error);
                    }
                },
                null,
                immediate: true);
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
                SocketOutput.Write(
                    _continueBytes,
                    (error, _) =>
                    {
                        if (error != null)
                        {
                            Log.LogError("ProduceContinue ", error);
                        }
                    },
                    null);
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
            SocketOutput.Write(
                responseHeader.Item1,
                (error, state) =>
                {
                    if (error != null)
                    {
                        Log.LogError("ProduceStart ", error);
                    }
                    ((IDisposable)state).Dispose();
                },
                responseHeader.Item2,
                immediate: immediate);
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

                    ResponseHeaders = new FrameResponseHeaders();
                    ResponseHeaders["Content-Length"] = new[] { "0" };
                }
            }

            ProduceStart(immediate: true, appCompleted: true);

            // _autoChunk should be checked after we are sure ProduceStart() has been called
            // since ProduceStart() may set _autoChunk to true.
            if (_autoChunk)
            {
                WriteChunkedResponseSuffix();
            }

            if (!_keepAlive)
            {
                ConnectionControl.End(ProduceEndType.SocketShutdownSend);
            }

            //NOTE: must finish reading request body
            ConnectionControl.End(_keepAlive ? ProduceEndType.ConnectionKeepAlive : ProduceEndType.SocketDisconnect);
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

        private bool TakeStartLine(SocketInput baton)
        {
            var remaining = baton.Buffer;
            if (remaining.Count < 2)
            {
                return false;
            }
            var firstSpace = -1;
            var secondSpace = -1;
            var questionMark = -1;
            var ch0 = remaining.Array[remaining.Offset];
            for (var index = 0; index != remaining.Count - 1; ++index)
            {
                var ch1 = remaining.Array[remaining.Offset + index + 1];
                if (ch0 == '\r' && ch1 == '\n')
                {
                    if (secondSpace == -1)
                    {
                        throw new InvalidOperationException("INVALID REQUEST FORMAT");
                    }
                    Method = GetString(remaining, 0, firstSpace);
                    RequestUri = GetString(remaining, firstSpace + 1, secondSpace);
                    if (questionMark == -1)
                    {
                        Path = RequestUri;
                        QueryString = string.Empty;
                    }
                    else
                    {
                        Path = GetString(remaining, firstSpace + 1, questionMark);
                        QueryString = GetString(remaining, questionMark, secondSpace);
                    }
                    HttpVersion = GetString(remaining, secondSpace + 1, index);
                    baton.Skip(index + 2);
                    return true;
                }

                if (ch0 == ' ' && firstSpace == -1)
                {
                    firstSpace = index;
                }
                else if (ch0 == ' ' && firstSpace != -1 && secondSpace == -1)
                {
                    secondSpace = index;
                }
                else if (ch0 == '?' && firstSpace != -1 && questionMark == -1 && secondSpace == -1)
                {
                    questionMark = index;
                }
                ch0 = ch1;
            }
            return false;
        }

        static string GetString(ArraySegment<byte> range, int startIndex, int endIndex)
        {
            return Encoding.UTF8.GetString(range.Array, range.Offset + startIndex, endIndex - startIndex);
        }


        private bool TakeMessageHeader(SocketInput baton, out bool endOfHeaders)
        {
            var remaining = baton.Buffer;
            endOfHeaders = false;
            if (remaining.Count < 2)
            {
                return false;
            }
            var ch0 = remaining.Array[remaining.Offset];
            var ch1 = remaining.Array[remaining.Offset + 1];
            if (ch0 == '\r' && ch1 == '\n')
            {
                endOfHeaders = true;
                baton.Skip(2);
                return true;
            }

            if (remaining.Count < 3)
            {
                return false;
            }
            var wrappedHeaders = false;
            var colonIndex = -1;
            var valueStartIndex = -1;
            var valueEndIndex = -1;
            for (var index = 0; index != remaining.Count - 2; ++index)
            {
                var ch2 = remaining.Array[remaining.Offset + index + 2];
                if (ch0 == '\r' &&
                    ch1 == '\n' &&
                        ch2 != ' ' &&
                            ch2 != '\t')
                {
                    var value = "";
                    if (valueEndIndex != -1)
                    {
                        value = _ascii.GetString(
                            remaining.Array, remaining.Offset + valueStartIndex, valueEndIndex - valueStartIndex);
                    }
                    if (wrappedHeaders)
                    {
                        value = value.Replace("\r\n", " ");
                    }
                    AddRequestHeader(remaining.Array, remaining.Offset, colonIndex, value);
                    baton.Skip(index + 2);
                    return true;
                }
                if (colonIndex == -1 && ch0 == ':')
                {
                    colonIndex = index;
                }
                else if (colonIndex != -1 &&
                    ch0 != ' ' &&
                        ch0 != '\t' &&
                            ch0 != '\r' &&
                                ch0 != '\n')
                {
                    if (valueStartIndex == -1)
                    {
                        valueStartIndex = index;
                    }
                    valueEndIndex = index + 1;
                }
                else if (!wrappedHeaders &&
                    ch0 == '\r' &&
                        ch1 == '\n' &&
                            (ch2 == ' ' ||
                                ch2 == '\t'))
                {
                    wrappedHeaders = true;
                }

                ch0 = ch1;
                ch1 = ch2;
            }
            return false;
        }

        private void AddRequestHeader(byte[] keyBytes, int keyOffset, int keyLength, string value)
        {
            _requestHeaders.Append(keyBytes, keyOffset, keyLength, value);
        }

        public bool StatusCanHaveBody(int statusCode)
        {
            // List of status codes taken from Microsoft.Net.Http.Server.Response
            return statusCode != 101 &&
                   statusCode != 204 &&
                   statusCode != 205 &&
                   statusCode != 304;
        }

        private enum Mode
        {
            StartLine,
            MessageHeader,
            MessageBody,
            Terminated,
        }
    }
}
