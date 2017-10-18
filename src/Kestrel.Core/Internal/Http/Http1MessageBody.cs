// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public abstract class Http1MessageBody : MessageBody
    {
        private readonly Http1Connection _context;

        private volatile bool _canceled;
        private Task _pumpTask;

        protected Http1MessageBody(Http1Connection context)
            : base(context)
        {
            _context = context;
        }

        private async Task PumpAsync()
        {
            Exception error = null;

            try
            {
                var awaitable = _context.Input.ReadAsync();

                if (!awaitable.IsCompleted)
                {
                    TryProduceContinue();
                }

                TryStartTimingReads();

                while (true)
                {
                    var result = await awaitable;

                    if (_context.RequestTimedOut)
                    {
                        _context.ThrowRequestRejected(RequestRejectionReason.RequestTimeout);
                    }

                    var readableBuffer = result.Buffer;
                    var consumed = readableBuffer.Start;
                    var examined = readableBuffer.End;

                    try
                    {
                        if (_canceled)
                        {
                            break;
                        }

                        if (!readableBuffer.IsEmpty)
                        {
                            var writableBuffer = _context.RequestBodyPipe.Writer.Alloc(1);
                            bool done;

                            try
                            {
                                done = Read(readableBuffer, writableBuffer, out consumed, out examined);
                            }
                            finally
                            {
                                writableBuffer.Commit();
                            }

                            var writeAwaitable = writableBuffer.FlushAsync();
                            var backpressure = false;

                            if (!writeAwaitable.IsCompleted)
                            {
                                // Backpressure, stop controlling incoming data rate until data is read.
                                backpressure = true;
                                TryPauseTimingReads();
                            }

                            await writeAwaitable;

                            if (backpressure)
                            {
                                TryResumeTimingReads();
                            }

                            if (done)
                            {
                                break;
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            _context.ThrowRequestRejected(RequestRejectionReason.UnexpectedEndOfRequestContent);
                        }

                        awaitable = _context.Input.ReadAsync();
                    }
                    finally
                    {
                        _context.Input.Advance(consumed, examined);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                _context.RequestBodyPipe.Writer.Complete(error);
                TryStopTimingReads();
            }
        }

        public override Task StopAsync()
        {
            if (!_context.HasStartedConsumingRequestBody)
            {
                return Task.CompletedTask;
            }

            _canceled = true;
            _context.Input.CancelPendingRead();
            return _pumpTask;
        }

        protected override async Task OnConsumeAsync()
        {
            _context.TimeoutControl.SetTimeout(Constants.RequestBodyDrainTimeout.Ticks, TimeoutAction.SendTimeoutResponse);

            try
            {
                ReadResult result;
                do
                {
                    result = await _context.RequestBodyPipe.Reader.ReadAsync();
                    _context.RequestBodyPipe.Reader.Advance(result.Buffer.End);
                } while (!result.IsCompleted);
            }
            finally
            {
                _context.TimeoutControl.CancelTimeout();
            }
        }

        protected void Copy(ReadableBuffer readableBuffer, WritableBuffer writableBuffer)
        {
            _context.TimeoutControl.BytesRead(readableBuffer.Length);

            if (readableBuffer.IsSingleSpan)
            {
                writableBuffer.Write(readableBuffer.First.Span);
            }
            else
            {
                foreach (var memory in readableBuffer)
                {
                    writableBuffer.Write(memory.Span);
                }
            }
        }

        protected override void OnReadStarted()
        {
            _pumpTask = PumpAsync();
        }

        protected virtual bool Read(ReadableBuffer readableBuffer, WritableBuffer writableBuffer, out ReadCursor consumed, out ReadCursor examined)
        {
            throw new NotImplementedException();
        }

        private void TryStartTimingReads()
        {
            if (!RequestUpgrade)
            {
                Log.RequestBodyStart(_context.ConnectionIdFeature, _context.TraceIdentifier);
                _context.TimeoutControl.StartTimingReads();
            }
        }

        private void TryPauseTimingReads()
        {
            if (!RequestUpgrade)
            {
                _context.TimeoutControl.PauseTimingReads();
            }
        }

        private void TryResumeTimingReads()
        {
            if (!RequestUpgrade)
            {
                _context.TimeoutControl.ResumeTimingReads();
            }
        }

        private void TryStopTimingReads()
        {
            if (!RequestUpgrade)
            {
                Log.RequestBodyDone(_context.ConnectionIdFeature, _context.TraceIdentifier);
                _context.TimeoutControl.StopTimingReads();
            }
        }

        public static MessageBody For(
            HttpVersion httpVersion,
            HttpRequestHeaders headers,
            Http1Connection context)
        {
            // see also http://tools.ietf.org/html/rfc2616#section-4.4
            var keepAlive = httpVersion != HttpVersion.Http10;

            var connection = headers.HeaderConnection;
            var upgrade = false;
            if (connection.Count > 0)
            {
                var connectionOptions = HttpHeaders.ParseConnection(connection);

                upgrade = (connectionOptions & ConnectionOptions.Upgrade) == ConnectionOptions.Upgrade;
                keepAlive = (connectionOptions & ConnectionOptions.KeepAlive) == ConnectionOptions.KeepAlive;
            }

            var transferEncoding = headers.HeaderTransferEncoding;
            if (transferEncoding.Count > 0)
            {
                var transferCoding = HttpHeaders.GetFinalTransferCoding(headers.HeaderTransferEncoding);

                // https://tools.ietf.org/html/rfc7230#section-3.3.3
                // If a Transfer-Encoding header field
                // is present in a request and the chunked transfer coding is not
                // the final encoding, the message body length cannot be determined
                // reliably; the server MUST respond with the 400 (Bad Request)
                // status code and then close the connection.
                if (transferCoding != TransferCoding.Chunked)
                {
                    context.ThrowRequestRejected(RequestRejectionReason.FinalTransferCodingNotChunked, transferEncoding.ToString());
                }

                if (upgrade)
                {
                    context.ThrowRequestRejected(RequestRejectionReason.UpgradeRequestCannotHavePayload);
                }

                return new ForChunkedEncoding(keepAlive, context);
            }

            if (headers.ContentLength.HasValue)
            {
                var contentLength = headers.ContentLength.Value;

                if (contentLength == 0)
                {
                    return keepAlive ? MessageBody.ZeroContentLengthKeepAlive : MessageBody.ZeroContentLengthClose;
                }
                else if (upgrade)
                {
                    context.ThrowRequestRejected(RequestRejectionReason.UpgradeRequestCannotHavePayload);
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
                    context.ThrowRequestRejected(requestRejectionReason, context.Method);
                }
            }

            if (upgrade)
            {
                return new ForUpgrade(context);
            }

            return keepAlive ? MessageBody.ZeroContentLengthKeepAlive : MessageBody.ZeroContentLengthClose;
        }

        private class ForUpgrade : Http1MessageBody
        {
            public ForUpgrade(Http1Connection context)
                : base(context)
            {
                RequestUpgrade = true;
            }

            protected override bool Read(ReadableBuffer readableBuffer, WritableBuffer writableBuffer, out ReadCursor consumed, out ReadCursor examined)
            {
                Copy(readableBuffer, writableBuffer);
                consumed = readableBuffer.End;
                examined = readableBuffer.End;
                return false;
            }
        }

        private class ForContentLength : Http1MessageBody
        {
            private readonly long _contentLength;
            private long _inputLength;

            public ForContentLength(bool keepAlive, long contentLength, Http1Connection context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
                _contentLength = contentLength;
                _inputLength = _contentLength;
            }

            protected override bool Read(ReadableBuffer readableBuffer, WritableBuffer writableBuffer, out ReadCursor consumed, out ReadCursor examined)
            {
                if (_inputLength == 0)
                {
                    throw new InvalidOperationException("Attempted to read from completed Content-Length request body.");
                }

                var actual = (int)Math.Min(readableBuffer.Length, _inputLength);
                _inputLength -= actual;

                consumed = readableBuffer.Move(readableBuffer.Start, actual);
                examined = consumed;

                Copy(readableBuffer.Slice(0, actual), writableBuffer);

                return _inputLength == 0;
            }

            protected override void OnReadStarting()
            {
                if (_contentLength > _context.MaxRequestBodySize)
                {
                    _context.ThrowRequestRejected(RequestRejectionReason.RequestBodyTooLarge);
                }
            }
        }

        /// <summary>
        ///   http://tools.ietf.org/html/rfc2616#section-3.6.1
        /// </summary>
        private class ForChunkedEncoding : Http1MessageBody
        {
            // byte consts don't have a data type annotation so we pre-cast it
            private const byte ByteCR = (byte)'\r';
            // "7FFFFFFF\r\n" is the largest chunk size that could be returned as an int.
            private const int MaxChunkPrefixBytes = 10;

            private long _inputLength;
            private long _consumedBytes;

            private Mode _mode = Mode.Prefix;

            public ForChunkedEncoding(bool keepAlive, Http1Connection context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
            }

            protected override bool Read(ReadableBuffer readableBuffer, WritableBuffer writableBuffer, out ReadCursor consumed, out ReadCursor examined)
            {
                consumed = default(ReadCursor);
                examined = default(ReadCursor);

                while (_mode < Mode.Trailer)
                {
                    if (_mode == Mode.Prefix)
                    {
                        ParseChunkedPrefix(readableBuffer, out consumed, out examined);

                        if (_mode == Mode.Prefix)
                        {
                            return false;
                        }

                        readableBuffer = readableBuffer.Slice(consumed);
                    }

                    if (_mode == Mode.Extension)
                    {
                        ParseExtension(readableBuffer, out consumed, out examined);

                        if (_mode == Mode.Extension)
                        {
                            return false;
                        }

                        readableBuffer = readableBuffer.Slice(consumed);
                    }

                    if (_mode == Mode.Data)
                    {
                        ReadChunkedData(readableBuffer, writableBuffer, out consumed, out examined);

                        if (_mode == Mode.Data)
                        {
                            return false;
                        }

                        readableBuffer = readableBuffer.Slice(consumed);
                    }

                    if (_mode == Mode.Suffix)
                    {
                        ParseChunkedSuffix(readableBuffer, out consumed, out examined);

                        if (_mode == Mode.Suffix)
                        {
                            return false;
                        }

                        readableBuffer = readableBuffer.Slice(consumed);
                    }
                }

                // Chunks finished, parse trailers
                if (_mode == Mode.Trailer)
                {
                    ParseChunkedTrailer(readableBuffer, out consumed, out examined);

                    if (_mode == Mode.Trailer)
                    {
                        return false;
                    }

                    readableBuffer = readableBuffer.Slice(consumed);
                }

                // _consumedBytes aren't tracked for trailer headers, since headers have seperate limits.
                if (_mode == Mode.TrailerHeaders)
                {
                    if (_context.TakeMessageHeaders(readableBuffer, out consumed, out examined))
                    {
                        _mode = Mode.Complete;
                    }
                }

                return _mode == Mode.Complete;
            }

            private void AddAndCheckConsumedBytes(long consumedBytes)
            {
                _consumedBytes += consumedBytes;

                if (_consumedBytes > _context.MaxRequestBodySize)
                {
                    _context.ThrowRequestRejected(RequestRejectionReason.RequestBodyTooLarge);
                }
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

                while (reader.ConsumedBytes < MaxChunkPrefixBytes)
                {
                    if (ch1 == ';')
                    {
                        consumed = reader.Cursor;
                        examined = reader.Cursor;

                        AddAndCheckConsumedBytes(reader.ConsumedBytes);
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

                        AddAndCheckConsumedBytes(reader.ConsumedBytes);
                        _inputLength = chunkSize;
                        _mode = chunkSize > 0 ? Mode.Data : Mode.Trailer;
                        return;
                    }

                    chunkSize = CalculateChunkSize(ch1, chunkSize);
                    ch1 = ch2;
                }

                // At this point, 10 bytes have been consumed which is enough to parse the max value "7FFFFFFF\r\n".
                _context.ThrowRequestRejected(RequestRejectionReason.BadChunkSizeData);
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
                        consumed = buffer.End;
                        examined = buffer.End;
                        AddAndCheckConsumedBytes(buffer.Length);
                        return;
                    };

                    var charsToByteCRExclusive = buffer.Slice(0, extensionCursor).Length;

                    var sufixBuffer = buffer.Slice(extensionCursor);
                    if (sufixBuffer.Length < 2)
                    {
                        consumed = extensionCursor;
                        examined = buffer.End;
                        AddAndCheckConsumedBytes(charsToByteCRExclusive);
                        return;
                    }

                    sufixBuffer = sufixBuffer.Slice(0, 2);
                    var sufixSpan = sufixBuffer.ToSpan();

                    if (sufixSpan[1] == '\n')
                    {
                        // We consumed the \r\n at the end of the extension, so switch modes.
                        _mode = _inputLength > 0 ? Mode.Data : Mode.Trailer;

                        consumed = sufixBuffer.End;
                        examined = sufixBuffer.End;
                        AddAndCheckConsumedBytes(charsToByteCRExclusive + 2);
                    }
                    else
                    {
                        // Don't consume suffixSpan[1] in case it is also a \r.
                        buffer = buffer.Slice(charsToByteCRExclusive + 1);
                        consumed = extensionCursor;
                        AddAndCheckConsumedBytes(charsToByteCRExclusive + 1);
                    }
                } while (_mode == Mode.Extension);
            }

            private void ReadChunkedData(ReadableBuffer buffer, WritableBuffer writableBuffer, out ReadCursor consumed, out ReadCursor examined)
            {
                var actual = Math.Min(buffer.Length, _inputLength);
                consumed = buffer.Move(buffer.Start, actual);
                examined = consumed;

                Copy(buffer.Slice(0, actual), writableBuffer);

                _inputLength -= actual;
                AddAndCheckConsumedBytes(actual);

                if (_inputLength == 0)
                {
                    _mode = Mode.Suffix;
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

                var suffixBuffer = buffer.Slice(0, 2);
                var suffixSpan = suffixBuffer.ToSpan();
                if (suffixSpan[0] == '\r' && suffixSpan[1] == '\n')
                {
                    consumed = suffixBuffer.End;
                    examined = suffixBuffer.End;
                    AddAndCheckConsumedBytes(2);
                    _mode = Mode.Prefix;
                }
                else
                {
                    _context.ThrowRequestRejected(RequestRejectionReason.BadChunkSuffix);
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
                    AddAndCheckConsumedBytes(2);
                    _mode = Mode.Complete;
                }
                else
                {
                    _mode = Mode.TrailerHeaders;
                }
            }

            private int CalculateChunkSize(int extraHexDigit, int currentParsedSize)
            {
                try
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
                }
                catch (OverflowException ex)
                {
                    throw new IOException(CoreStrings.BadRequest_BadChunkSizeData, ex);
                }

                _context.ThrowRequestRejected(RequestRejectionReason.BadChunkSizeData);
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
