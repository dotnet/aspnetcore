// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public abstract class MessageBody
    {
        private readonly FrameContext _context;
        private int _send100Continue = 1;

        protected MessageBody(FrameContext context)
        {
            _context = context;
        }

        public bool RequestKeepAlive { get; protected set; }

        public ValueTask<int> ReadAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            var send100Continue = 0;
            var result = ReadAsyncImplementation(buffer, cancellationToken);
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

        public Task Consume(CancellationToken cancellationToken = default(CancellationToken))
        {
            ValueTask<int> result;
            var send100checked = false;
            do
            {
                result = ReadAsyncImplementation(default(ArraySegment<byte>), cancellationToken);
                if (!result.IsCompleted)
                {
                    if (!send100checked)
                    {
                        if (Interlocked.Exchange(ref _send100Continue, 0) == 1)
                        {
                            _context.FrameControl.ProduceContinue();
                        }
                        send100checked = true;
                    }
                    // Incomplete Task await result
                    return ConsumeAwaited(result.AsTask(), cancellationToken);
                }
                // ValueTask uses .GetAwaiter().GetResult() if necessary
                else if (result.Result == 0) 
                {
                    // Completed Task, end of stream
                    return TaskUtilities.CompletedTask;
                }

            } while (true);
        }

        private async Task ConsumeAwaited(Task<int> currentTask, CancellationToken cancellationToken)
        {
            if (await currentTask == 0)
            {
                return;
            }

            ValueTask<int> result;
            do
            {
                result = ReadAsyncImplementation(default(ArraySegment<byte>), cancellationToken);
                if (result.IsCompleted)
                {
                    // ValueTask uses .GetAwaiter().GetResult() if necessary
                    if (result.Result == 0)
                    {
                        // Completed Task, end of stream
                        return;
                    }
                    else
                    {
                        // Completed Task, get next Task rather than await
                        continue;
                    }
                }
            } while (await result != 0);
        }

        public abstract ValueTask<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken);

        public static MessageBody For(
            string httpVersion,
            FrameRequestHeaders headers,
            FrameContext context)
        {
            // see also http://tools.ietf.org/html/rfc2616#section-4.4

            var keepAlive = httpVersion != "HTTP/1.0";

            var connection = headers.HeaderConnection.ToString();
            if (connection.Length > 0)
            {
                keepAlive = connection.Equals("keep-alive", StringComparison.OrdinalIgnoreCase);
            }

            var transferEncoding = headers.HeaderTransferEncoding.ToString();
            if (transferEncoding.Length > 0)
            {
                return new ForChunkedEncoding(keepAlive, context);
            }

            var contentLength = headers.HeaderContentLength.ToString();
            if (contentLength.Length > 0)
            {
                return new ForContentLength(keepAlive, int.Parse(contentLength), context);
            }

            if (keepAlive)
            {
                return new ForContentLength(true, 0, context);
            }

            return new ForRemainingData(context);
        }

        private class ForRemainingData : MessageBody
        {
            public ForRemainingData(FrameContext context)
                : base(context)
            {
            }

            public override ValueTask<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                return _context.SocketInput.ReadAsync(buffer.Array, buffer.Offset, buffer.Array == null ? 8192 : buffer.Count);
            }
        }

        private class ForContentLength : MessageBody
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

            public override ValueTask<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                var input = _context.SocketInput;

                var limit = buffer.Array == null ? _inputLength : Math.Min(buffer.Count, _inputLength);
                if (limit == 0)
                {
                    return 0;
                }

                var task = _context.SocketInput.ReadAsync(buffer.Array, buffer.Offset, limit);

                if (task.IsCompleted)
                {
                    // .GetAwaiter().GetResult() done by ValueTask if needed
                    var actual = task.Result;
                    _inputLength -= actual;
                    if (actual == 0)
                    {
                        throw new InvalidDataException("Unexpected end of request content");
                    }
                    return actual;
                }
                else
                {
                    return ReadAsyncAwaited(task.AsTask());
                }
            }

            private async Task<int> ReadAsyncAwaited(Task<int> task)
            {
                var actual = await task;
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
        private class ForChunkedEncoding : MessageBody
        {
            private int _inputLength;

            private Mode _mode = Mode.ChunkPrefix;

            public ForChunkedEncoding(bool keepAlive, FrameContext context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
            }
            public override ValueTask<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                return ReadAsyncAwaited(buffer, cancellationToken);
            }

            private async Task<int> ReadAsyncAwaited(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                var input = _context.SocketInput;

                while (_mode != Mode.Trailer && _mode != Mode.Complete)
                {
                    while (_mode == Mode.ChunkPrefix)
                    {
                        ReadChunkedPrefix(input);
                        await input;
                    }

                    while (_mode == Mode.ChunkData)
                    {
                        int actual = ReadChunkedData(input, buffer.Array, buffer.Offset, buffer.Count);
                        if (actual != 0)
                        {
                            return actual;
                        }

                        await input;
                    }

                    while (_mode == Mode.ChunkSuffix)
                    {
                        ReadChunkedSuffix(input);
                        await input;
                    }
                }

                while (_mode == Mode.Trailer)
                {
                    ReadChunkedTrailer(input);
                    await input;
                }

                return 0;
            }

            private void ReadChunkedPrefix(SocketInput input)
            {
                int chunkSize;
                if (TakeChunkedLine(input, out chunkSize))
                {
                    if (chunkSize == 0)
                    {
                        _mode = Mode.Trailer;
                    }
                    else
                    {
                        _mode = Mode.ChunkData;
                    }
                    _inputLength = chunkSize;
                }
                else if (input.RemoteIntakeFin)
                {
                    ThrowChunkedRequestIncomplete();
                }
            }

            private int ReadChunkedData(SocketInput input, byte[] buffer, int offset, int count)
            {
                var scan = input.ConsumingStart();
                int actual;
                try
                {
                    var limit = buffer == null ? _inputLength : Math.Min(count, _inputLength);
                    scan = scan.CopyTo(buffer, offset, limit, out actual);
                    _inputLength -= actual;
                }
                finally
                {
                    input.ConsumingComplete(scan, scan);
                }

                if (_inputLength == 0)
                {
                    _mode = Mode.ChunkSuffix;
                }
                else if (actual == 0 && input.RemoteIntakeFin)
                {
                    ThrowChunkedRequestIncomplete();
                }

                return actual;
            }

            private void ReadChunkedSuffix(SocketInput input)
            {
                var scan = input.ConsumingStart();
                var consumed = scan;
                try
                {
                    var ch1 = scan.Take();
                    var ch2 = scan.Take();

                    if (ch1 == '\r' && ch2 == '\n')
                    {
                        consumed = scan;
                        _mode = Mode.ChunkPrefix;
                    }
                    else if (ch1 == -1 || ch2 == -1)
                    {
                        if (input.RemoteIntakeFin)
                        {
                            ThrowChunkedRequestIncomplete();
                        }
                    }
                    else
                    {
                        ThrowInvalidFormat();
                    }
                }
                finally
                {
                    input.ConsumingComplete(consumed, scan);
                }
            }

            private void ReadChunkedTrailer(SocketInput input)
            {
                var scan = input.ConsumingStart();
                var consumed = scan;
                try
                {
                    var ch1 = scan.Take();
                    var ch2 = scan.Take();

                    if (ch1 == '\r' && ch2 == '\n')
                    {
                        consumed = scan;
                        _mode = Mode.Complete;
                    }
                    else if (ch1 == -1 || ch2 == -1)
                    {
                        if (input.RemoteIntakeFin)
                        {
                            ThrowChunkedRequestIncomplete();
                        }
                    }
                    else
                    {
                        // Post request headers
                        ThrowTrailingHeadersNotSupported();
                    }
                }
                finally
                {
                    input.ConsumingComplete(consumed, scan);
                }
            }

            private static bool TakeChunkedLine(SocketInput baton, out int chunkSizeOut)
            {
                var scan = baton.ConsumingStart();
                var consumed = scan;
                try
                {
                    var ch0 = scan.Take();
                    var chunkSize = 0;
                    var mode = Mode.ChunkPrefix;
                    while (ch0 != -1)
                    {
                        var ch1 = scan.Take();
                        if (ch1 == -1)
                        {
                            chunkSizeOut = 0;
                            return false;
                        }

                        if (mode == Mode.ChunkPrefix)
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
                                ThrowInvalidFormat();
                            }
                            mode = Mode.ChunkData;
                        }
                        else if (mode == Mode.ChunkData)
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
                                mode = Mode.ChunkSuffix;
                            }
                            else if (ch0 == '\r' && ch1 == '\n')
                            {
                                consumed = scan;
                                chunkSizeOut = chunkSize;
                                return true;
                            }
                            else
                            {
                                ThrowInvalidFormat();
                            }
                        }
                        else if (mode == Mode.ChunkSuffix)
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
                                ThrowChunkedExtensionsNotSupported();
                            }
                        }

                        ch0 = ch1;
                    }
                    chunkSizeOut = 0;
                    return false;
                }
                finally
                {
                    baton.ConsumingComplete(consumed, scan);
                }
            }

            private static void ThrowInvalidFormat()
            {
                throw new InvalidOperationException("Bad Request");
            }

            private static void ThrowChunkedRequestIncomplete()
            {
                throw new InvalidOperationException("Chunked request incomplete");
            }

            private static void ThrowChunkedExtensionsNotSupported()
            {
                throw new NotImplementedException("Chunked-extensions not supported");
            }

            private static void ThrowTrailingHeadersNotSupported()
            {
                throw new NotImplementedException("Trailing headers not supported");
            }

            private enum Mode
            {
                ChunkPrefix,
                ChunkData,
                ChunkSuffix,
                Trailer,
                Complete
            };
        }
    }
}
