// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public abstract class MessageBody
    {
        private readonly Frame _context;
        private int _send100Continue = 1;

        protected MessageBody(Frame context)
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
                    return TaskCache.CompletedTask;
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
            HttpVersion httpVersion,
            FrameRequestHeaders headers,
            Frame context)
        {
            // see also http://tools.ietf.org/html/rfc2616#section-4.4
            var keepAlive = httpVersion != HttpVersion.Http10;

            var connection = headers.HeaderConnection.ToString();
            if (connection.Length > 0)
            {
                keepAlive = connection.Equals("keep-alive", StringComparison.OrdinalIgnoreCase);
            }

            var transferEncoding = headers.HeaderTransferEncoding.ToString();
            if (transferEncoding.Length > 0)
            {
                return new ForChunkedEncoding(keepAlive, headers, context);
            }

            var unparsedContentLength = headers.HeaderContentLength.ToString();
            if (unparsedContentLength.Length > 0)
            {
                long contentLength;
                if (!long.TryParse(unparsedContentLength, out contentLength) || contentLength < 0)
                {
                    context.RejectRequest(RequestRejectionReason.InvalidContentLength, unparsedContentLength);
                }
                else
                {
                    return new ForContentLength(keepAlive, contentLength, context);
                }
            }

            if (keepAlive)
            {
                return new ForContentLength(true, 0, context);
            }

            return new ForRemainingData(context);
        }

        private class ForRemainingData : MessageBody
        {
            public ForRemainingData(Frame context)
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
            private readonly long _contentLength;
            private long _inputLength;

            public ForContentLength(bool keepAlive, long contentLength, Frame context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
                _contentLength = contentLength;
                _inputLength = _contentLength;
            }

            public override ValueTask<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                var input = _context.SocketInput;

                var inputLengthLimit = (int)Math.Min(_inputLength, int.MaxValue);
                var limit = buffer.Array == null ? inputLengthLimit : Math.Min(buffer.Count, inputLengthLimit);
                if (limit == 0)
                {
                    return new ValueTask<int>(0);
                }

                var task = _context.SocketInput.ReadAsync(buffer.Array, buffer.Offset, limit);

                if (task.IsCompleted)
                {
                    // .GetAwaiter().GetResult() done by ValueTask if needed
                    var actual = task.Result;
                    _inputLength -= actual;

                    if (actual == 0)
                    {
                        _context.RejectRequest(RequestRejectionReason.UnexpectedEndOfRequestContent);
                    }

                    return new ValueTask<int>(actual);
                }
                else
                {
                    return new ValueTask<int>(ReadAsyncAwaited(task.AsTask()));
                }
            }

            private async Task<int> ReadAsyncAwaited(Task<int> task)
            {
                var actual = await task;
                _inputLength -= actual;

                if (actual == 0)
                {
                    _context.RejectRequest(RequestRejectionReason.UnexpectedEndOfRequestContent);
                }

                return actual;
            }
        }

        /// <summary>
        ///   http://tools.ietf.org/html/rfc2616#section-3.6.1
        /// </summary>
        private class ForChunkedEncoding : MessageBody
        {
            // This causes an InvalidProgramException if made static
            // https://github.com/dotnet/corefx/issues/8825
            private Vector<byte> _vectorCRs = new Vector<byte>((byte)'\r');

            private int _inputLength;
            private Mode _mode = Mode.Prefix;
            private FrameRequestHeaders _requestHeaders;

            public ForChunkedEncoding(bool keepAlive, FrameRequestHeaders headers, Frame context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
                _requestHeaders = headers;
            }

            public override ValueTask<int> ReadAsyncImplementation(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                return new ValueTask<int>(ReadStateMachineAsync(_context.SocketInput, buffer, cancellationToken));
            }

            private async Task<int> ReadStateMachineAsync(SocketInput input, ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                while (_mode < Mode.Trailer)
                {
                    while (_mode == Mode.Prefix)
                    {
                        var fin = input.CheckFinOrThrow();

                        ParseChunkedPrefix(input);

                        if (_mode != Mode.Prefix)
                        {
                            break;
                        }
                        else if (fin)
                        {
                            _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                        }

                        await input;
                    }

                    while (_mode == Mode.Extension)
                    {
                        var fin = input.CheckFinOrThrow();

                        ParseExtension(input);

                        if (_mode != Mode.Extension)
                        {
                            break;
                        }
                        else if (fin)
                        {
                            _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                        }

                        await input;
                    }

                    while (_mode == Mode.Data)
                    {
                        var fin = input.CheckFinOrThrow();

                        int actual = ReadChunkedData(input, buffer.Array, buffer.Offset, buffer.Count);

                        if (actual != 0)
                        {
                            return actual;
                        }
                        else if (_mode != Mode.Data)
                        {
                            break;
                        }
                        else if (fin)
                        {
                            _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                        }

                        await input;
                    }

                    while (_mode == Mode.Suffix)
                    {
                        var fin = input.CheckFinOrThrow();

                        ParseChunkedSuffix(input);

                        if (_mode != Mode.Suffix)
                        {
                            break;
                        }
                        else if (fin)
                        {
                            _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                        }

                        await input;
                    }
                }

                // Chunks finished, parse trailers
                while (_mode == Mode.Trailer)
                {
                    var fin = input.CheckFinOrThrow();

                    ParseChunkedTrailer(input);

                    if (_mode != Mode.Trailer)
                    {
                        break;
                    }
                    else if (fin)
                    {
                        _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                    }

                    await input;
                }

                if (_mode == Mode.TrailerHeaders)
                {
                    while (!_context.TakeMessageHeaders(input, _requestHeaders))
                    {
                        if (input.CheckFinOrThrow())
                        {
                            if (_context.TakeMessageHeaders(input, _requestHeaders))
                            {
                                break;
                            }
                            else
                            {
                                _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                            }
                        }

                        await input;
                    }

                    _mode = Mode.Complete;
                }

                return 0;
            }

            private void ParseChunkedPrefix(SocketInput input)
            {
                var scan = input.ConsumingStart();
                var consumed = scan;
                try
                {
                    var ch1 = scan.Take();
                    var ch2 = scan.Take();
                    if (ch1 == -1 || ch2 == -1)
                    {
                        return;
                    }

                    var chunkSize = CalculateChunkSize(ch1, 0);
                    ch1 = ch2;

                    do
                    {
                        if (ch1 == ';')
                        {
                            consumed = scan;

                            _inputLength = chunkSize;
                            _mode = Mode.Extension;
                            return;
                        }

                        ch2 = scan.Take();
                        if (ch2 == -1)
                        {
                            return;
                        }

                        if (ch1 == '\r' && ch2 == '\n')
                        {
                            consumed = scan;
                            _inputLength = chunkSize;

                            if (chunkSize > 0)
                            {
                                _mode = Mode.Data;
                            }
                            else
                            {
                                _mode = Mode.Trailer;
                            }

                            return;
                        }

                        chunkSize = CalculateChunkSize(ch1, chunkSize);
                        ch1 = ch2;
                    } while (ch1 != -1);
                }
                finally
                {
                    input.ConsumingComplete(consumed, scan);
                }
            }

            private void ParseExtension(SocketInput input)
            {
                var scan = input.ConsumingStart();
                var consumed = scan;
                try
                {
                    // Chunk-extensions not currently parsed
                    // Just drain the data
                    do
                    {
                        if (scan.Seek(ref _vectorCRs) == -1)
                        {
                            // End marker not found yet
                            consumed = scan;
                            return;
                        };

                        var ch1 = scan.Take();
                        var ch2 = scan.Take();

                        if (ch2 == '\n')
                        {
                            consumed = scan;
                            if (_inputLength > 0)
                            {
                                _mode = Mode.Data;
                            }
                            else
                            {
                                _mode = Mode.Trailer;
                            }
                        }
                        else if (ch2 == -1)
                        {
                            return;
                        }
                    } while (_mode == Mode.Extension);
                }
                finally
                {
                    input.ConsumingComplete(consumed, scan);
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
                    _mode = Mode.Suffix;
                }

                return actual;
            }

            private void ParseChunkedSuffix(SocketInput input)
            {
                var scan = input.ConsumingStart();
                var consumed = scan;
                try
                {
                    var ch1 = scan.Take();
                    var ch2 = scan.Take();
                    if (ch1 == -1 || ch2 == -1)
                    {
                        return;
                    }
                    else if (ch1 == '\r' && ch2 == '\n')
                    {
                        consumed = scan;
                        _mode = Mode.Prefix;
                    }
                    else
                    {
                        _context.RejectRequest(RequestRejectionReason.BadChunkSuffix);
                    }
                }
                finally
                {
                    input.ConsumingComplete(consumed, scan);
                }
            }

            private void ParseChunkedTrailer(SocketInput input)
            {
                var scan = input.ConsumingStart();
                var consumed = scan;
                try
                {
                    var ch1 = scan.Take();
                    var ch2 = scan.Take();

                    if (ch1 == -1 || ch2 == -1)
                    {
                        return;
                    }
                    else if (ch1 == '\r' && ch2 == '\n')
                    {
                        consumed = scan;
                        _mode = Mode.Complete;
                    }
                    else
                    {
                        _mode = Mode.TrailerHeaders;
                    }
                }
                finally
                {
                    input.ConsumingComplete(consumed, scan);
                }
            }

            private int CalculateChunkSize(int extraHexDigit, int currentParsedSize)
            {
                checked
                {
                    if (extraHexDigit >= '0' && extraHexDigit <= '9')
                    {
                        return currentParsedSize * 0x10 + (extraHexDigit - '0');
                    }
                    else if (extraHexDigit >= 'A' && extraHexDigit <= 'F')
                    {
                        return currentParsedSize * 0x10 + (extraHexDigit - ('A' - 10));
                    }
                    else if (extraHexDigit >= 'a' && extraHexDigit <= 'f')
                    {
                        return currentParsedSize * 0x10 + (extraHexDigit - ('a' - 10));
                    }
                }

                _context.RejectRequest(RequestRejectionReason.BadChunkSizeData);
                return -1; // can't happen, but compiler complains
            }

            private enum Mode
            {
                Prefix,
                Extension,
                Data,
                Suffix,
                Trailer,
                TrailerHeaders,
                Complete
            };
        }
    }
}
