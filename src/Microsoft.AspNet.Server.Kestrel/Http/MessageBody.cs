// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public abstract class MessageBody
    {
        private FrameContext _context;
        private int _send100Continue = 1;

        protected MessageBody(FrameContext context)
        {
            _context = context;
        }

        public bool RequestKeepAlive { get; protected set; }

        public Task<int> ReadAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            Task<int> result = null;
            var send100Continue = 0;
            result = ReadAsyncImplementation(buffer, cancellationToken);
            if (!result.IsCompleted)
            {
                send100Continue = Interlocked.Exchange(ref _send100Continue, 0);
            }
            if (send100Continue == 1)
            {
                _context.FrameControl.ProduceContinue();
            }
            return result;
        }

        public Task<int> SkipAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Task<int> result = null;
            var send100Continue = 0;
            result = SkipAsyncImplementation(cancellationToken);
            if (!result.IsCompleted)
            {
                send100Continue = Interlocked.Exchange(ref _send100Continue, 0);
            }
            if (send100Continue == 1)
            {
                _context.FrameControl.ProduceContinue();
            }
            return result;
        }

        public abstract Task<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken);

        public abstract Task<int> SkipAsyncImplementation(CancellationToken cancellationToken);

        public static MessageBody For(
            string httpVersion,
            IDictionary<string, StringValues> headers,
            FrameContext context)
        {
            // see also http://tools.ietf.org/html/rfc2616#section-4.4

            var keepAlive = httpVersion != "HTTP/1.0";

            string connection;
            if (TryGet(headers, "Connection", out connection))
            {
                keepAlive = connection.Equals("keep-alive", StringComparison.OrdinalIgnoreCase);
            }

            string transferEncoding;
            if (TryGet(headers, "Transfer-Encoding", out transferEncoding))
            {
                return new ForChunkedEncoding(keepAlive, context);
            }

            string contentLength;
            if (TryGet(headers, "Content-Length", out contentLength))
            {
                return new ForContentLength(keepAlive, int.Parse(contentLength), context);
            }

            if (keepAlive)
            {
                return new ForContentLength(true, 0, context);
            }

            return new ForRemainingData(context);
        }

        public static bool TryGet(IDictionary<string, StringValues> headers, string name, out string value)
        {
            StringValues values;
            if (!headers.TryGetValue(name, out values) || values.Count == 0)
            {
                value = null;
                return false;
            }
            var count = values.Count;
            if (count == 0)
            {
                value = null;
                return false;
            }
            if (count == 1)
            {
                value = values[0];
                return true;
            }
            value = string.Join(",", values.ToArray());
            return true;
        }


        class ForRemainingData : MessageBody
        {
            public ForRemainingData(FrameContext context)
                : base(context)
            {
            }

            public override Task<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                return _context.SocketInput.ReadAsync(buffer);
            }
            public override Task<int> SkipAsyncImplementation(CancellationToken cancellationToken)
            {
                return _context.SocketInput.SkipAsync(4096);
            }
        }

        class ForContentLength : MessageBody
        {
            private readonly int _contentLength;
            private int _inputLength;

            public ForContentLength(bool keepAlive, int contentLength, FrameContext context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
                _contentLength = contentLength;
                _inputLength = _contentLength;
            }

            public override async Task<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                var input = _context.SocketInput;

                var limit = Math.Min(buffer.Count, _inputLength);
                if (limit == 0)
                {
                    return 0;
                }

                var limitedBuffer = new ArraySegment<byte>(buffer.Array, buffer.Offset, limit);
                var actual = await _context.SocketInput.ReadAsync(limitedBuffer);
                _inputLength -= actual;

                if (actual == 0)
                {
                    throw new InvalidDataException("Unexpected end of request content");
                }

                return actual;
            }

            public override async Task<int> SkipAsyncImplementation(CancellationToken cancellationToken)
            {
                var input = _context.SocketInput;

                var limit = Math.Min(4096, _inputLength);
                if (limit == 0)
                {
                    return 0;
                }

                var actual = await _context.SocketInput.SkipAsync(limit);
                _inputLength -= actual;

                if (actual == 0)
                {
                    throw new InvalidDataException("Unexpected end of request content");
                }

                return actual;
            }
        }


        /// <summary>
        ///   http://tools.ietf.org/html/rfc2616#section-3.6.1
        /// </summary>
        class ForChunkedEncoding : MessageBody
        {
            private int _inputLength;

            private Mode _mode = Mode.ChunkPrefix;

            public ForChunkedEncoding(bool keepAlive, FrameContext context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
            }

            public override async Task<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                var input = _context.SocketInput;

                while (_mode != Mode.Complete)
                {
                    while (_mode == Mode.ChunkPrefix)
                    {
                        var chunkSize = 0;
                        if (!TakeChunkedLine(input, ref chunkSize))
                        {
                            await input;
                        }
                        else if (chunkSize == 0)
                        {
                            _mode = Mode.Complete;
                        }
                        else
                        {
                            _mode = Mode.ChunkData;
                        }
                        _inputLength = chunkSize;
                    }
                    while (_mode == Mode.ChunkData)
                    {
                        var limit = Math.Min(buffer.Count, _inputLength);
                        if (limit != 0)
                        {
                            await input;
                        }

                        var begin = input.ConsumingStart();
                        int actual;
                        var end = begin.CopyTo(buffer.Array, buffer.Offset, limit, out actual);
                        _inputLength -= actual;
                        input.ConsumingComplete(end, end);

                        if (_inputLength == 0)
                        {
                            _mode = Mode.ChunkSuffix;
                        }
                        if (actual != 0)
                        {
                            return actual;
                        }
                    }
                    while (_mode == Mode.ChunkSuffix)
                    {
                        var scan = input.ConsumingStart();
                        var consumed = scan;
                        var ch1 = scan.Take();
                        var ch2 = scan.Take();
                        if (ch1 == -1 || ch2 == -1)
                        {
                            input.ConsumingComplete(consumed, scan);
                            await input;
                        }
                        else if (ch1 == '\r' && ch2 == '\n')
                        {
                            input.ConsumingComplete(scan, scan);
                            _mode = Mode.ChunkPrefix;
                        }
                        else
                        {
                            throw new NotImplementedException("INVALID REQUEST FORMAT");
                        }
                    }
                }

                return 0;
            }

            public override async Task<int> SkipAsyncImplementation(CancellationToken cancellationToken)
            {
                var input = _context.SocketInput;

                while (_mode != Mode.Complete)
                {
                    while (_mode == Mode.ChunkPrefix)
                    {
                        var chunkSize = 0;
                        if (!TakeChunkedLine(input, ref chunkSize))
                        {
                            await input;
                        }
                        else if (chunkSize == 0)
                        {
                            _mode = Mode.Complete;
                        }
                        else
                        {
                            _mode = Mode.ChunkData;
                        }
                        _inputLength = chunkSize;
                    }
                    while (_mode == Mode.ChunkData)
                    {
                        var limit = Math.Min(4096, _inputLength);
                        if (limit != 0)
                        {
                            await input;
                        }

                        var begin = input.ConsumingStart();
                        int actual;
                        var end = begin.Skip(limit, out actual);
                        _inputLength -= actual;
                        input.ConsumingComplete(end, end);

                        if (_inputLength == 0)
                        {
                            _mode = Mode.ChunkSuffix;
                        }
                        if (actual != 0)
                        {
                            return actual;
                        }
                    }
                    while (_mode == Mode.ChunkSuffix)
                    {
                        var scan = input.ConsumingStart();
                        var consumed = scan;
                        var ch1 = scan.Take();
                        var ch2 = scan.Take();
                        if (ch1 == -1 || ch2 == -1)
                        {
                            input.ConsumingComplete(consumed, scan);
                            await input;
                        }
                        else if (ch1 == '\r' && ch2 == '\n')
                        {
                            input.ConsumingComplete(scan, scan);
                            _mode = Mode.ChunkPrefix;
                        }
                        else
                        {
                            throw new NotImplementedException("INVALID REQUEST FORMAT");
                        }
                    }
                }

                return 0;
            }

            private static bool TakeChunkedLine(SocketInput baton, ref int chunkSizeOut)
            {
                var scan = baton.ConsumingStart();
                var consumed = scan;
                try
                {
                    var ch0 = scan.Take();
                    var chunkSize = 0;
                    var mode = 0;
                    while (ch0 != -1)
                    {
                        var ch1 = scan.Take();
                        if (ch1 == -1)
                        {
                            return false;
                        }

                        if (mode == 0)
                        {
                            if (ch0 >= '0' && ch0 <= '9')
                            {
                                chunkSize = chunkSize * 0x10 + (ch0 - '0');
                            }
                            else if (ch0 >= 'A' && ch0 <= 'F')
                            {
                                chunkSize = chunkSize * 0x10 + (ch0 - ('A' - 10));
                            }
                            else if (ch0 >= 'a' && ch0 <= 'f')
                            {
                                chunkSize = chunkSize * 0x10 + (ch0 - ('a' - 10));
                            }
                            else
                            {
                                throw new NotImplementedException("INVALID REQUEST FORMAT");
                            }
                            mode = 1;
                        }
                        else if (mode == 1)
                        {
                            if (ch0 >= '0' && ch0 <= '9')
                            {
                                chunkSize = chunkSize * 0x10 + (ch0 - '0');
                            }
                            else if (ch0 >= 'A' && ch0 <= 'F')
                            {
                                chunkSize = chunkSize * 0x10 + (ch0 - ('A' - 10));
                            }
                            else if (ch0 >= 'a' && ch0 <= 'f')
                            {
                                chunkSize = chunkSize * 0x10 + (ch0 - ('a' - 10));
                            }
                            else if (ch0 == ';')
                            {
                                mode = 2;
                            }
                            else if (ch0 == '\r' && ch1 == '\n')
                            {
                                consumed = scan;
                                chunkSizeOut = chunkSize;
                                return true;
                            }
                            else
                            {
                                throw new NotImplementedException("INVALID REQUEST FORMAT");
                            }
                        }
                        else if (mode == 2)
                        {
                            if (ch0 == '\r' && ch1 == '\n')
                            {
                                consumed = scan;
                                chunkSizeOut = chunkSize;
                                return true;
                            }
                            else
                            {
                                // chunk-extensions not currently parsed
                            }
                        }

                        ch0 = ch1;
                    }
                    return false;
                }
                finally
                {
                    baton.ConsumingComplete(consumed, scan);
                }
            }

            private enum Mode
            {
                ChunkPrefix,
                ChunkData,
                ChunkSuffix,
                Complete,
            };
        }
    }
}
