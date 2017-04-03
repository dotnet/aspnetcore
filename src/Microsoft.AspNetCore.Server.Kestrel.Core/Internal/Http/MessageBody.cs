// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public abstract class MessageBody
    {
        private static readonly MessageBody _zeroContentLengthClose = new ForZeroContentLength(keepAlive: false);
        private static readonly MessageBody _zeroContentLengthKeepAlive = new ForZeroContentLength(keepAlive: true);

        private readonly Frame _context;
        private bool _send100Continue = true;

        protected MessageBody(Frame context)
        {
            _context = context;
        }

        public bool RequestKeepAlive { get; protected set; }

        public bool RequestUpgrade { get; protected set; }

        public Task<int> ReadAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            var task = PeekAsync(cancellationToken);

            if (!task.IsCompleted)
            {
                TryProduceContinue();

                // Incomplete Task await result
                return ReadAsyncAwaited(task, buffer);
            }
            else
            {
                var readSegment = task.Result;
                var consumed = CopyReadSegment(readSegment, buffer);

                return consumed == 0 ? TaskCache<int>.DefaultCompletedTask : Task.FromResult(consumed);
            }
        }

        private async Task<int> ReadAsyncAwaited(ValueTask<ArraySegment<byte>> currentTask, ArraySegment<byte> buffer)
        {
            return CopyReadSegment(await currentTask, buffer);
        }

        private int CopyReadSegment(ArraySegment<byte> readSegment, ArraySegment<byte> buffer)
        {
            var consumed = Math.Min(readSegment.Count, buffer.Count);

            if (consumed != 0)
            {
                Buffer.BlockCopy(readSegment.Array, readSegment.Offset, buffer.Array, buffer.Offset, consumed);
                ConsumedBytes(consumed);
            }

            return consumed;
        }

        public Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default(CancellationToken))
        {
            var peekTask = PeekAsync(cancellationToken);

            while (peekTask.IsCompleted)
            {
                // ValueTask uses .GetAwaiter().GetResult() if necessary
                var segment = peekTask.Result;

                if (segment.Count == 0)
                {
                    return TaskCache.CompletedTask;
                }

                Task destinationTask;
                try
                {
                    destinationTask = destination.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);
                }
                catch
                {
                    ConsumedBytes(segment.Count);
                    throw;
                }

                if (!destinationTask.IsCompleted)
                {
                    return CopyToAsyncDestinationAwaited(destinationTask, segment.Count, destination, cancellationToken);
                }

                ConsumedBytes(segment.Count);

                // Surface errors if necessary
                destinationTask.GetAwaiter().GetResult();

                peekTask = PeekAsync(cancellationToken);
            }

            TryProduceContinue();

            return CopyToAsyncPeekAwaited(peekTask, destination, cancellationToken);
        }

        private async Task CopyToAsyncPeekAwaited(
            ValueTask<ArraySegment<byte>> peekTask,
            Stream destination,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            while (true)
            {
                var segment = await peekTask;

                if (segment.Count == 0)
                {
                    return;
                }

                try
                {
                    await destination.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken);
                }
                finally
                {
                    ConsumedBytes(segment.Count);
                }

                peekTask = PeekAsync(cancellationToken);
            }
        }

        private async Task CopyToAsyncDestinationAwaited(
            Task destinationTask,
            int bytesConsumed,
            Stream destination,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await destinationTask;
            }
            finally
            {
                ConsumedBytes(bytesConsumed);
            }

            var peekTask = PeekAsync(cancellationToken);

            if (!peekTask.IsCompleted)
            {
                TryProduceContinue();
            }

            await CopyToAsyncPeekAwaited(peekTask, destination, cancellationToken);
        }

        public Task Consume(CancellationToken cancellationToken = default(CancellationToken))
        {
            while (true)
            {
                var task = PeekAsync(cancellationToken);
                if (!task.IsCompleted)
                {
                    TryProduceContinue();

                    // Incomplete Task await result
                    return ConsumeAwaited(task, cancellationToken);
                }
                else
                {
                    // ValueTask uses .GetAwaiter().GetResult() if necessary
                    if (task.Result.Count == 0)
                    {
                        // Completed Task, end of stream
                        return TaskCache.CompletedTask;
                    }

                    ConsumedBytes(task.Result.Count);
                }
            }
        }

        private async Task ConsumeAwaited(ValueTask<ArraySegment<byte>> currentTask, CancellationToken cancellationToken)
        {
            while (true)
            {
                var count = (await currentTask).Count;

                if (count == 0)
                {
                    // Completed Task, end of stream
                    return;
                }

                ConsumedBytes(count);
                currentTask = PeekAsync(cancellationToken);
            }
        }

        private void TryProduceContinue()
        {
            if (_send100Continue)
            {
                _context.FrameControl.ProduceContinue();
                _send100Continue = false;
            }
        }

        private void ConsumedBytes(int count)
        {
            var scan = _context.Input.ReadAsync().GetResult().Buffer;
            var consumed = scan.Move(scan.Start, count);
            _context.Input.Advance(consumed, consumed);

            OnConsumedBytes(count);
        }

        protected abstract ValueTask<ArraySegment<byte>> PeekAsync(CancellationToken cancellationToken);

        protected virtual void OnConsumedBytes(int count)
        {
        }

        public static MessageBody For(
            HttpVersion httpVersion,
            FrameRequestHeaders headers,
            Frame context)
        {
            // see also http://tools.ietf.org/html/rfc2616#section-4.4
            var keepAlive = httpVersion != HttpVersion.Http10;

            var connection = headers.HeaderConnection;
            if (connection.Count > 0)
            {
                var connectionOptions = FrameHeaders.ParseConnection(connection);

                if ((connectionOptions & ConnectionOptions.Upgrade) == ConnectionOptions.Upgrade)
                {
                    return new ForRemainingData(true, context);
                }

                keepAlive = (connectionOptions & ConnectionOptions.KeepAlive) == ConnectionOptions.KeepAlive;
            }

            var transferEncoding = headers.HeaderTransferEncoding;
            if (transferEncoding.Count > 0)
            {
                var transferCoding = FrameHeaders.GetFinalTransferCoding(headers.HeaderTransferEncoding);

                // https://tools.ietf.org/html/rfc7230#section-3.3.3
                // If a Transfer-Encoding header field
                // is present in a request and the chunked transfer coding is not
                // the final encoding, the message body length cannot be determined
                // reliably; the server MUST respond with the 400 (Bad Request)
                // status code and then close the connection.
                if (transferCoding != TransferCoding.Chunked)
                {
                    context.RejectRequest(RequestRejectionReason.FinalTransferCodingNotChunked, transferEncoding.ToString());
                }

                return new ForChunkedEncoding(keepAlive, headers, context);
            }

            if (headers.ContentLength.HasValue)
            {
                var contentLength = headers.ContentLength.Value;
                if (contentLength == 0)
                {
                    return keepAlive ? _zeroContentLengthKeepAlive : _zeroContentLengthClose;
                }

                return new ForContentLength(keepAlive, contentLength, context);
            }

            // Avoid slowing down most common case
            if (!object.ReferenceEquals(context.Method, HttpMethods.Get))
            {
                // If we got here, request contains no Content-Length or Transfer-Encoding header.
                // Reject with 411 Length Required.
                if (HttpMethods.IsPost(context.Method) || HttpMethods.IsPut(context.Method))
                {
                    var requestRejectionReason = httpVersion == HttpVersion.Http11 ? RequestRejectionReason.LengthRequired : RequestRejectionReason.LengthRequiredHttp10;
                    context.RejectRequest(requestRejectionReason, context.Method);
                }
            }

            return keepAlive ? _zeroContentLengthKeepAlive : _zeroContentLengthClose;
        }

        private class ForRemainingData : MessageBody
        {
            public ForRemainingData(bool upgrade, Frame context)
                : base(context)
            {
                RequestUpgrade = upgrade;
            }

            protected override ValueTask<ArraySegment<byte>> PeekAsync(CancellationToken cancellationToken)
            {
                return _context.Input.PeekAsync();
            }
        }

        private class ForZeroContentLength : MessageBody
        {
            public ForZeroContentLength(bool keepAlive)
                : base(null)
            {
                RequestKeepAlive = keepAlive;
            }

            protected override ValueTask<ArraySegment<byte>> PeekAsync(CancellationToken cancellationToken)
            {
                return new ValueTask<ArraySegment<byte>>();
            }

            protected override void OnConsumedBytes(int count)
            {
                if (count > 0)
                {
                    throw new InvalidDataException("Consuming non-existent data");
                }
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

            protected override ValueTask<ArraySegment<byte>> PeekAsync(CancellationToken cancellationToken)
            {
                var limit = (int)Math.Min(_inputLength, int.MaxValue);
                if (limit == 0)
                {
                    return new ValueTask<ArraySegment<byte>>();
                }

                var task = _context.Input.PeekAsync();

                if (task.IsCompleted)
                {
                    // .GetAwaiter().GetResult() done by ValueTask if needed
                    var actual = Math.Min(task.Result.Count, limit);

                    if (task.Result.Count == 0)
                    {
                        _context.RejectRequest(RequestRejectionReason.UnexpectedEndOfRequestContent);
                    }

                    if (task.Result.Count < _inputLength)
                    {
                        return task;
                    }
                    else
                    {
                        var result = task.Result;
                        var part = new ArraySegment<byte>(result.Array, result.Offset, (int)_inputLength);
                        return new ValueTask<ArraySegment<byte>>(part);
                    }
                }
                else
                {
                    return new ValueTask<ArraySegment<byte>>(PeekAsyncAwaited(task));
                }
            }

            private async Task<ArraySegment<byte>> PeekAsyncAwaited(ValueTask<ArraySegment<byte>> task)
            {
                var segment = await task;

                if (segment.Count == 0)
                {
                    _context.RejectRequest(RequestRejectionReason.UnexpectedEndOfRequestContent);
                }

                if (segment.Count <= _inputLength)
                {
                    return segment;
                }
                else
                {
                    return new ArraySegment<byte>(segment.Array, segment.Offset, (int)_inputLength);
                }
            }

            protected override void OnConsumedBytes(int count)
            {
                _inputLength -= count;
            }
        }

        /// <summary>
        ///   http://tools.ietf.org/html/rfc2616#section-3.6.1
        /// </summary>
        private class ForChunkedEncoding : MessageBody
        {
            // byte consts don't have a data type annotation so we pre-cast it
            private const byte ByteCR = (byte)'\r';

            private readonly IPipeReader _input;
            private readonly FrameRequestHeaders _requestHeaders;
            private int _inputLength;

            private Mode _mode = Mode.Prefix;

            public ForChunkedEncoding(bool keepAlive, FrameRequestHeaders headers, Frame context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
                _input = _context.Input;
                _requestHeaders = headers;
            }

            protected override ValueTask<ArraySegment<byte>> PeekAsync(CancellationToken cancellationToken)
            {
                return new ValueTask<ArraySegment<byte>>(PeekStateMachineAsync());
            }

            protected override void OnConsumedBytes(int count)
            {
                _inputLength -= count;
            }

            private async Task<ArraySegment<byte>> PeekStateMachineAsync()
            {
                while (_mode < Mode.Trailer)
                {
                    while (_mode == Mode.Prefix)
                    {
                        var result = await _input.ReadAsync();
                        var buffer = result.Buffer;
                        var consumed = default(ReadCursor);
                        var examined = default(ReadCursor);

                        try
                        {
                            ParseChunkedPrefix(buffer, out consumed, out examined);
                        }
                        finally
                        {
                            _input.Advance(consumed, examined);
                        }

                        if (_mode != Mode.Prefix)
                        {
                            break;
                        }
                        else if (result.IsCompleted)
                        {
                            _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                        }

                    }

                    while (_mode == Mode.Extension)
                    {
                        var result = await _input.ReadAsync();
                        var buffer = result.Buffer;
                        var consumed = default(ReadCursor);
                        var examined = default(ReadCursor);

                        try
                        {
                            ParseExtension(buffer, out consumed, out examined);
                        }
                        finally
                        {
                            _input.Advance(consumed, examined);
                        }

                        if (_mode != Mode.Extension)
                        {
                            break;
                        }
                        else if (result.IsCompleted)
                        {
                            _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                        }

                    }

                    while (_mode == Mode.Data)
                    {
                        var result = await _input.ReadAsync();
                        var buffer = result.Buffer;
                        ArraySegment<byte> segment;
                        try
                        {
                            segment = PeekChunkedData(buffer);
                        }
                        finally
                        {
                            _input.Advance(buffer.Start, buffer.Start);
                        }

                        if (segment.Count != 0)
                        {
                            return segment;
                        }
                        else if (_mode != Mode.Data)
                        {
                            break;
                        }
                        else if (result.IsCompleted)
                        {
                            _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                        }
                    }

                    while (_mode == Mode.Suffix)
                    {
                        var result = await _input.ReadAsync();
                        var buffer = result.Buffer;
                        var consumed = default(ReadCursor);
                        var examined = default(ReadCursor);

                        try
                        {
                            ParseChunkedSuffix(buffer, out consumed, out examined);
                        }
                        finally
                        {
                            _input.Advance(consumed, examined);
                        }

                        if (_mode != Mode.Suffix)
                        {
                            break;
                        }
                        else if (result.IsCompleted)
                        {
                            _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                        }
                    }
                }

                // Chunks finished, parse trailers
                while (_mode == Mode.Trailer)
                {
                    var result = await _input.ReadAsync();
                    var buffer = result.Buffer;
                    var consumed = default(ReadCursor);
                    var examined = default(ReadCursor);

                    try
                    {
                        ParseChunkedTrailer(buffer, out consumed, out examined);
                    }
                    finally
                    {
                        _input.Advance(consumed, examined);
                    }

                    if (_mode != Mode.Trailer)
                    {
                        break;
                    }
                    else if (result.IsCompleted)
                    {
                        _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                    }

                }

                if (_mode == Mode.TrailerHeaders)
                {
                    while (true)
                    {
                        var result = await _input.ReadAsync();
                        var buffer = result.Buffer;

                        if (buffer.IsEmpty && result.IsCompleted)
                        {
                            _context.RejectRequest(RequestRejectionReason.ChunkedRequestIncomplete);
                        }

                        var consumed = default(ReadCursor);
                        var examined = default(ReadCursor);

                        try
                        {
                            if (_context.TakeMessageHeaders(buffer, out consumed, out examined))
                            {
                                break;
                            }
                        }
                        finally
                        {
                            _input.Advance(consumed, examined);
                        }
                    }
                    _mode = Mode.Complete;
                }

                return default(ArraySegment<byte>);
            }

            private void ParseChunkedPrefix(ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined)
            {
                consumed = buffer.Start;
                examined = buffer.Start;
                var reader = new ReadableBufferReader(buffer);
                var ch1 = reader.Take();
                var ch2 = reader.Take();

                if (ch1 == -1 || ch2 == -1)
                {
                    examined = reader.Cursor;
                    return;
                }

                var chunkSize = CalculateChunkSize(ch1, 0);
                ch1 = ch2;

                do
                {
                    if (ch1 == ';')
                    {
                        consumed = reader.Cursor;
                        examined = reader.Cursor;

                        _inputLength = chunkSize;
                        _mode = Mode.Extension;
                        return;
                    }

                    ch2 = reader.Take();
                    if (ch2 == -1)
                    {
                        examined = reader.Cursor;
                        return;
                    }

                    if (ch1 == '\r' && ch2 == '\n')
                    {
                        consumed = reader.Cursor;
                        examined = reader.Cursor;

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

            private void ParseExtension(ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined)
            {
                // Chunk-extensions not currently parsed
                // Just drain the data
                consumed = buffer.Start;
                examined = buffer.Start;
                do
                {
                    ReadCursor extensionCursor;
                    if (ReadCursorOperations.Seek(buffer.Start, buffer.End, out extensionCursor, ByteCR) == -1)
                    {
                        // End marker not found yet
                        examined = buffer.End;
                        return;
                    };

                    var sufixBuffer = buffer.Slice(extensionCursor);
                    if (sufixBuffer.Length < 2)
                    {
                        examined = buffer.End;
                        return;
                    }

                    sufixBuffer = sufixBuffer.Slice(0, 2);
                    var sufixSpan = sufixBuffer.ToSpan();


                    if (sufixSpan[1] == '\n')
                    {
                        consumed = sufixBuffer.End;
                        examined = sufixBuffer.End;
                        if (_inputLength > 0)
                        {
                            _mode = Mode.Data;
                        }
                        else
                        {
                            _mode = Mode.Trailer;
                        }
                    }
                } while (_mode == Mode.Extension);
            }

            private ArraySegment<byte> PeekChunkedData(ReadableBuffer buffer)
            {
                if (_inputLength == 0)
                {
                    _mode = Mode.Suffix;
                    return default(ArraySegment<byte>);
                }
                var segment = buffer.First.GetArray();

                int actual = Math.Min(segment.Count, _inputLength);
                // Nothing is consumed yet. ConsumedBytes(int) will move the iterator.
                if (actual == segment.Count)
                {
                    return segment;
                }
                else
                {
                    return new ArraySegment<byte>(segment.Array, segment.Offset, actual);
                }
            }

            private void ParseChunkedSuffix(ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined)
            {
                consumed = buffer.Start;
                examined = buffer.Start;

                if (buffer.Length < 2)
                {
                    examined = buffer.End;
                    return;
                }

                var sufixBuffer = buffer.Slice(0, 2);
                var sufixSpan = sufixBuffer.ToSpan();
                if (sufixSpan[0] == '\r' && sufixSpan[1] == '\n')
                {
                    consumed = sufixBuffer.End;
                    examined = sufixBuffer.End;
                    _mode = Mode.Prefix;
                }
                else
                {
                    _context.RejectRequest(RequestRejectionReason.BadChunkSuffix);
                }
            }

            private void ParseChunkedTrailer(ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined)
            {
                consumed = buffer.Start;
                examined = buffer.Start;

                if (buffer.Length < 2)
                {
                    examined = buffer.End;
                    return;
                }

                var trailerBuffer = buffer.Slice(0, 2);
                var trailerSpan = trailerBuffer.ToSpan();

                if (trailerSpan[0] == '\r' && trailerSpan[1] == '\n')
                {
                    consumed = trailerBuffer.End;
                    examined = trailerBuffer.End;
                    _mode = Mode.Complete;
                }
                else
                {
                    _mode = Mode.TrailerHeaders;
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
