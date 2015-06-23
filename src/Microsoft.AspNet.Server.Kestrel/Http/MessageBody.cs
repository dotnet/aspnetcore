// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public abstract class MessageBody : MessageBodyExchanger
    {
        public bool RequestKeepAlive { get; protected set; }

        protected MessageBody(FrameContext context) : base(context)
        {
        }

        public void Intake(int count)
        {
            Transfer(count, false);
        }

        public void IntakeFin(int count)
        {
            Transfer(count, true);
        }

        public abstract void Consume();

        public static MessageBody For(
            string httpVersion,
            IDictionary<string, string[]> headers,
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

        public static bool TryGet(IDictionary<string, string[]> headers, string name, out string value)
        {
            string[] values;
            if (!headers.TryGetValue(name, out values) || values == null)
            {
                value = null;
                return false;
            }
            var count = values.Length;
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

            public override void Consume()
            {
                var input = _context.SocketInput;

                if (input.RemoteIntakeFin)
                {
                    IntakeFin(input.Buffer.Count);
                }
                else
                {
                    Intake(input.Buffer.Count);
                }
            }
        }

        class ForContentLength : MessageBody
        {
            private readonly int _contentLength;
            private int _neededLength;

            public ForContentLength(bool keepAlive, int contentLength, FrameContext context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
                _contentLength = contentLength;
                _neededLength = _contentLength;
            }

            public override void Consume()
            {
                var input = _context.SocketInput;
                var consumeLength = Math.Min(_neededLength, input.Buffer.Count);
                _neededLength -= consumeLength;

                if (_neededLength != 0)
                {
                    Intake(consumeLength);
                }
                else
                {
                    IntakeFin(consumeLength);
                }
            }
        }


        /// <summary>
        ///   http://tools.ietf.org/html/rfc2616#section-3.6.1
        /// </summary>
        class ForChunkedEncoding : MessageBody
        {
            private int _neededLength;

            private Mode _mode = Mode.ChunkSizeLine;

            private enum Mode
            {
                ChunkSizeLine,
                ChunkData,
                ChunkDataCRLF,
                Complete,
            };


            public ForChunkedEncoding(bool keepAlive, FrameContext context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
            }

            public override void Consume()
            {
                var input = _context.SocketInput;
                for (; ;)
                {
                    switch (_mode)
                    {
                        case Mode.ChunkSizeLine:
                            var chunkSize = 0;
                            if (!TakeChunkedLine(input, ref chunkSize))
                            {
                                return;
                            }

                            _neededLength = chunkSize;
                            if (chunkSize == 0)
                            {
                                _mode = Mode.Complete;
                                IntakeFin(0);
                                return;
                            }
                            _mode = Mode.ChunkData;
                            break;

                        case Mode.ChunkData:
                            if (_neededLength == 0)
                            {
                                _mode = Mode.ChunkDataCRLF;
                                break;
                            }
                            if (input.Buffer.Count == 0)
                            {
                                return;
                            }

                            var consumeLength = Math.Min(_neededLength, input.Buffer.Count);
                            _neededLength -= consumeLength;

                            Intake(consumeLength);
                            break;

                        case Mode.ChunkDataCRLF:
                            if (input.Buffer.Count < 2)
                            {
                                return;
                            }
                            var crlf = input.Take(2);
                            if (crlf.Array[crlf.Offset] != '\r' ||
                                crlf.Array[crlf.Offset + 1] != '\n')
                            {
                                throw new NotImplementedException("INVALID REQUEST FORMAT");
                            }
                            _mode = Mode.ChunkSizeLine;
                            break;

                        default:
                            throw new NotImplementedException("INVALID REQUEST FORMAT");
                    }
                }
            }

            private static bool TakeChunkedLine(SocketInput baton, ref int chunkSizeOut)
            {
                var remaining = baton.Buffer;
                if (remaining.Count < 2)
                {
                    return false;
                }
                var ch0 = remaining.Array[remaining.Offset];
                var chunkSize = 0;
                var mode = 0;
                for (var index = 0; index != remaining.Count - 1; ++index)
                {
                    var ch1 = remaining.Array[remaining.Offset + index + 1];

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
                            baton.Skip(index + 2);
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
                            baton.Skip(index + 2);
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
        }
    }
}
