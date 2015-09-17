// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public abstract class MessageBody : MessageBodyExchanger
    {
        protected MessageBody(FrameContext context) : base(context)
        {
        }

        public bool RequestKeepAlive { get; protected set; }

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
            if (!headers.TryGetValue(name, out values) || values == null)
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
            value = String.Join(",", values);
            return true;
        }


        class ForRemainingData : MessageBody
        {
            public ForRemainingData(FrameContext context)
                : base(context)
            {
            }

            public override async Task<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                var input = _context.SocketInput;
                while (true)
                {
                    await input;

                    var begin = input.GetIterator();
                    int actual;
                    var end = begin.CopyTo(buffer.Array, buffer.Offset, buffer.Count, out actual);
                    input.JumpTo(end);

                    if (actual != 0)
                    {
                        return actual;
                    }
                    if (input.RemoteIntakeFin)
                    {
                        return 0;
                    }
                }
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

                while (true)
                {
                    var limit = Math.Min(buffer.Count, _inputLength);
                    if (limit == 0)
                    {
                        return 0;
                    }

                    await input;

                    var begin = input.GetIterator();
                    int actual;
                    var end = begin.CopyTo(buffer.Array, buffer.Offset, limit, out actual);
                    _inputLength -= actual;
                    input.JumpTo(end);
                    if (actual != 0)
                    {
                        return actual;
                    }
                    if (input.RemoteIntakeFin)
                    {
                        throw new Exception("Unexpected end of request content");
                    }
                }
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

                        var begin = input.GetIterator();
                        int actual;
                        var end = begin.CopyTo(buffer.Array, buffer.Offset, limit, out actual);
                        _inputLength -= actual;
                        input.JumpTo(end);

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
                        var scan = input.GetIterator();
                        var ch1 = scan.Take();
                        var ch2 = scan.Take();
                        if (ch1 == -1 || ch2 == -1)
                        {
                            await input;
                        }
                        else if (ch1 == '\r' && ch2 == '\n')
                        {
                            input.JumpTo(scan);
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
                var scan = baton.GetIterator();
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
                            baton.JumpTo(scan);
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
                            baton.JumpTo(scan);
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
