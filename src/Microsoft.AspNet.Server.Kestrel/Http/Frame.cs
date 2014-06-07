// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.HttpFeature;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable AccessToModifiedClosure

namespace Microsoft.AspNet.Server.Kestrel.Http
{

    public enum ProduceEndType
    {
        SocketShutdownSend,
        SocketDisconnect,
        ConnectionKeepAlive,
    }

    public class Frame
    {
        private ConnectionContext _context;

        Mode _mode;

        enum Mode
        {
            StartLine,
            MessageHeader,
            MessageBody,
            Terminated,
        }


        private string _method;
        private string _requestUri;
        private string _path;
        private string _queryString;
        private string _httpVersion;

        private readonly IDictionary<string, string[]> _requestHeaders =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        readonly IDictionary<string, string[]> _responseHeaders =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        private MessageBody _messageBody;
        private bool _resultStarted;
        private bool _keepAlive;

        private CallContext _callContext;
        /*
        //IDictionary<string, object> _environment;

        CancellationTokenSource _cts = new CancellationTokenSource();
        */
        FrameResponseStream _outputStream;
        FrameRequestStream _inputStream;
        FrameDuplexStream _duplexStream;

        Task _upgradeTask = _completedTask;
        static readonly Task _completedTask = Task.FromResult(0);

        public Frame(ConnectionContext context)
        {
            _context = context;
        }
        /*
        public bool LocalIntakeFin
        {
            get
            {
                return _mode == Mode.MessageBody
                    ? _messageBody.LocalIntakeFin
                    : _mode == Mode.Terminated;
            }
        }
        */
        public void Consume()
        {
            var input = _context.SocketInput;
            for (; ;)
            {
                switch (_mode)
                {
                    case Mode.StartLine:
                        if (input.Buffer.Count == 0 && input.RemoteIntakeFin)
                        {
                            _mode = Mode.Terminated;
                            return;
                        }

                        if (!TakeStartLine(input))
                        {
                            if (input.RemoteIntakeFin)
                            {
                                _mode = Mode.Terminated;
                            }
                            return;
                        }

                        _mode = Mode.MessageHeader;
                        break;

                    case Mode.MessageHeader:
                        if (input.Buffer.Count == 0 && input.RemoteIntakeFin)
                        {
                            _mode = Mode.Terminated;
                            return;
                        }

                        var endOfHeaders = false;
                        while (!endOfHeaders)
                        {
                            if (!TakeMessageHeader(input, out endOfHeaders))
                            {
                                if (input.RemoteIntakeFin)
                                {
                                    _mode = Mode.Terminated;
                                }
                                return;
                            }
                        }

                        //var resumeBody = HandleExpectContinue(callback);
                        _mode = Mode.MessageBody;
                        Execute();
                        break;

                    case Mode.MessageBody:
                        if (_messageBody.LocalIntakeFin)
                        {
                            // NOTE: stop reading and resume on keepalive?
                            return;
                        }
                        _messageBody.Consume();
                        // NOTE: keep looping?
                        return;

                    case Mode.Terminated:
                        return;
                }
            }
        }

        Action<Frame, Exception> HandleExpectContinue(Action<Frame, Exception> continuation)
        {
            string[] expect;
            if (_httpVersion.Equals("HTTP/1.1") &&
                _requestHeaders.TryGetValue("Expect", out expect) &&
                    (expect.FirstOrDefault() ?? "").Equals("100-continue", StringComparison.OrdinalIgnoreCase))
            {
                return (frame, error) =>
                {
                    if (_resultStarted)
                    {
                        continuation.Invoke(frame, error);
                    }
                    else
                    {
                        var bytes = Encoding.Default.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");

                        //var isasync = _context.SocketOutput.Write(
                        //    new ArraySegment<byte>(bytes),
                        //    error2 => continuation(frame, error2));

                        //if (!isasync)
                        //{
                        //    continuation.Invoke(frame, null);
                        //}
                    }
                };
            }
            return continuation;
        }

        private void Execute()
        {
            _messageBody = MessageBody.For(
                _httpVersion,
                _requestHeaders,
                _context);
            _keepAlive = _messageBody.RequestKeepAlive;
            _callContext = CreateCallContext();
            _context.SocketInput.Free();
            Task.Run(ExecuteAsync);
        }

        private async Task ExecuteAsync()
        {
            Exception error = null;
            try
            {
                await _context.Application.Invoke(_callContext);
                await _upgradeTask;
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                ProduceEnd(error);
            }
        }

        private CallContext CreateCallContext()
        {
            _inputStream = new FrameRequestStream(_messageBody);
            _outputStream = new FrameResponseStream(OnWrite);
            _duplexStream = new FrameDuplexStream(_inputStream, _outputStream);

            var remoteIpAddress = "127.0.0.1";
            var remotePort = "0";
            var localIpAddress = "127.0.0.1";
            var localPort = "80";
            var isLocal = false;

            //if (_context.Socket != null)
            //{
            //    var remoteEndPoint = _context.Socket.RemoteEndPoint as IPEndPoint;
            //    if (remoteEndPoint != null)
            //    {
            //        remoteIpAddress = remoteEndPoint.Address.ToString();
            //        remotePort = remoteEndPoint.Port.ToString(CultureInfo.InvariantCulture);
            //    }

            //    var localEndPoint = _context.Socket.LocalEndPoint as IPEndPoint;
            //    if (localEndPoint != null)
            //    {
            //        localIpAddress = localEndPoint.Address.ToString();
            //        localPort = localEndPoint.Port.ToString(CultureInfo.InvariantCulture);
            //    }

            //    if (remoteEndPoint != null && localEndPoint != null)
            //    {
            //        isLocal = Equals(remoteEndPoint.Address, localEndPoint.Address);
            //    }
            //}

            var callContext = new CallContext();
            var request = (IHttpRequestFeature)callContext;
            var response = (IHttpResponseFeature)callContext;
            //var lifetime = (IHttpRequestLifetimeFeature)callContext;
            request.Protocol = _httpVersion;
            request.Scheme = "http";
            request.Method = _method;
            request.Path = _path;
            request.PathBase = "";
            request.QueryString = _queryString;
            request.Headers = _requestHeaders;
            request.Body = _inputStream;
            response.Headers = _responseHeaders;
            response.Body = _outputStream;

            //var env = new Dictionary<string, object>();
            //env["owin.Version"] = "1.0";
            //env["owin.RequestProtocol"] = _httpVersion;
            //env["owin.RequestScheme"] = "http";
            //env["owin.RequestMethod"] = _method;
            //env["owin.RequestPath"] = _path;
            //env["owin.RequestPathBase"] = "";
            //env["owin.RequestQueryString"] = _queryString;
            //env["owin.RequestHeaders"] = _requestHeaders;
            //env["owin.RequestBody"] = _inputStream;
            //env["owin.ResponseHeaders"] = _responseHeaders;
            //env["owin.ResponseBody"] = _outputStream;
            //env["owin.CallCancelled"] = _cts.Token;
            //env["opaque.Upgrade"] = (Action<IDictionary<string, object>, Func<IDictionary<string, object>, Task>>)Upgrade;
            //env["opaque.Stream"] = _duplexStream;
            //env["server.RemoteIpAddress"] = remoteIpAddress;
            //env["server.RemotePort"] = remotePort;
            //env["server.LocalIpAddress"] = localIpAddress;
            //env["server.LocalPort"] = localPort;
            //env["server.IsLocal"] = isLocal;
            return callContext;
        }

        void OnWrite(ArraySegment<byte> data, Action<object> callback, object state)
        {
            ProduceStart();
            _context.SocketOutput.Write(data, callback, state);
        }

        void Upgrade(IDictionary<string, object> options, Func<object, Task> callback)
        {
            _keepAlive = false;
            ProduceStart();

            _upgradeTask = callback(_callContext);
        }

        void ProduceStart()
        {
            if (_resultStarted) return;

            _resultStarted = true;

            var response = (IHttpResponseFeature)_callContext;
            var status = ReasonPhrases.ToStatus(
                response.StatusCode,
                response.ReasonPhrase);

            var responseHeader = CreateResponseHeader(status, _responseHeaders);
            _context.SocketOutput.Write(responseHeader.Item1, x => ((IDisposable)x).Dispose(), responseHeader.Item2);
        }

        private void ProduceEnd(Exception ex)
        {
            ProduceStart();

            if (!_keepAlive)
            {
                _context.ConnectionControl.End(ProduceEndType.SocketShutdownSend);
            }

            _messageBody.Drain(() =>
                _context.ConnectionControl.End(_keepAlive ? ProduceEndType.ConnectionKeepAlive : ProduceEndType.SocketDisconnect));
        }


        private Tuple<ArraySegment<byte>, IDisposable> CreateResponseHeader(
            string status, IEnumerable<KeyValuePair<string, string[]>> headers)
        {
            var writer = new MemoryPoolTextWriter(_context.Memory);
            writer.Write(_httpVersion);
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

            if (hasTransferEncoding == false && hasContentLength == false)
            {
                _keepAlive = false;
            }
            if (_keepAlive == false && hasConnection == false && _httpVersion == "HTTP/1.1")
            {
                writer.Write("Connection: close\r\n\r\n");
            }
            else if (_keepAlive && hasConnection == false && _httpVersion == "HTTP/1.0")
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
                    _method = GetString(remaining, 0, firstSpace);
                    _requestUri = GetString(remaining, firstSpace + 1, secondSpace);
                    if (questionMark == -1)
                    {
                        _path = _requestUri;
                        _queryString = string.Empty;
                    }
                    else
                    {
                        _path = GetString(remaining, firstSpace + 1, questionMark);
                        _queryString = GetString(remaining, questionMark, secondSpace);
                    }
                    _httpVersion = GetString(remaining, secondSpace + 1, index);
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
            return Encoding.Default.GetString(range.Array, range.Offset + startIndex, endIndex - startIndex);
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
                    var name = Encoding.ASCII.GetString(remaining.Array, remaining.Offset, colonIndex);
                    var value = "";
                    if (valueEndIndex != -1)
                    {
                        value = Encoding.ASCII.GetString(
                            remaining.Array, remaining.Offset + valueStartIndex, valueEndIndex - valueStartIndex);
                    }
                    if (wrappedHeaders)
                    {
                        value = value.Replace("\r\n", " ");
                    }
                    AddRequestHeader(name, value);
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

        private void AddRequestHeader(string name, string value)
        {
            string[] existing;
            if (!_requestHeaders.TryGetValue(name, out existing) ||
                existing == null ||
                existing.Length == 0)
            {
                _requestHeaders[name] = new[] { value };
            }
            else
            {
                _requestHeaders[name] = existing.Concat(new[] { value }).ToArray();
            }
        }
    }
}
