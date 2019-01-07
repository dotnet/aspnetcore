// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
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
            : base(context, context.MinRequestBodyDataRate)
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

                while (true)
                {
                    var result = await awaitable;

                    if (_context.RequestTimedOut)
                    {
                        BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTimeout);
                    }

                    var readableBuffer = result.Buffer;
                    var consumed = readableBuffer.Start;
                    var examined = readableBuffer.Start;

                    try
                    {
                        if (_canceled)
                        {
                            break;
                        }

                        if (!readableBuffer.IsEmpty)
                        {
                            bool done;
                            done = Read(readableBuffer, _context.RequestBodyPipe.Writer, out consumed, out examined);

                            await _context.RequestBodyPipe.Writer.FlushAsync();

                            if (done)
                            {
                                break;
                            }
                        }

                        // Read() will have already have greedily consumed the entire request body if able.
                        if (result.IsCompleted)
                        {
                            // OnInputOrOutputCompleted() is an idempotent method that closes the connection. Sometimes
                            // input completion is observed here before the Input.OnWriterCompleted() callback is fired,
                            // so we call OnInputOrOutputCompleted() now to prevent a race in our tests where a 400
                            // response is written after observing the unexpected end of request content instead of just
                            // closing the connection without a response as expected.
                            _context.OnInputOrOutputCompleted();

                            // Treat any FIN from an upgraded request as expected.
                            // It's up to higher-level consumer (i.e. WebSocket middleware) to determine
                            // if the end is actually expected based on higher-level framing.
                            if (RequestUpgrade)
                            {
                                break;
                            }

                            BadHttpRequestException.Throw(RequestRejectionReason.UnexpectedEndOfRequestContent);
                        }
                    }
                    finally
                    {
                        _context.Input.AdvanceTo(consumed, examined);
                    }

                    awaitable = _context.Input.ReadAsync();
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                _context.RequestBodyPipe.Writer.Complete(error);
            }
        }

        protected override Task OnStopAsync()
        {
            if (!_context.HasStartedConsumingRequestBody)
            {
                return Task.CompletedTask;
            }

            // PumpTask catches all Exceptions internally.
            if (_pumpTask.IsCompleted)
            {
                // At this point both the request body pipe reader and writer should be completed.
                _context.RequestBodyPipe.Reset();
                return Task.CompletedTask;
            }

            return StopAsyncAwaited();
        }

        private async Task StopAsyncAwaited()
        {
            _canceled = true;
            _context.Input.CancelPendingRead();
            await _pumpTask;

            // At this point both the request body pipe reader and writer should be completed.
            _context.RequestBodyPipe.Reset();
        }

        protected override Task OnConsumeAsync()
        {
            try
            {
                if (_context.RequestBodyPipe.Reader.TryRead(out var readResult))
                {
                    _context.RequestBodyPipe.Reader.AdvanceTo(readResult.Buffer.End);

                    if (readResult.IsCompleted)
                    {
                        return Task.CompletedTask;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // TryRead can throw OperationCanceledException https://github.com/dotnet/corefx/issues/32029
                // because of buggy logic, this works around that for now
            }
            catch (BadHttpRequestException ex)
            {
                // At this point, the response has already been written, so this won't result in a 4XX response;
                // however, we still need to stop the request processing loop and log.
                _context.SetBadRequestState(ex);
                return Task.CompletedTask;
            }

            return OnConsumeAsyncAwaited();
        }

        private async Task OnConsumeAsyncAwaited()
        {
            Log.RequestBodyNotEntirelyRead(_context.ConnectionIdFeature, _context.TraceIdentifier);

            _context.TimeoutControl.SetTimeout(Constants.RequestBodyDrainTimeout.Ticks, TimeoutReason.RequestBodyDrain);

            try
            {
                ReadResult result;
                do
                {
                    result = await _context.RequestBodyPipe.Reader.ReadAsync();
                    _context.RequestBodyPipe.Reader.AdvanceTo(result.Buffer.End);
                } while (!result.IsCompleted);
            }
            catch (BadHttpRequestException ex)
            {
                _context.SetBadRequestState(ex);
            }
            catch (ConnectionAbortedException)
            {
                Log.RequestBodyDrainTimedOut(_context.ConnectionIdFeature, _context.TraceIdentifier);
            }
            finally
            {
                _context.TimeoutControl.CancelTimeout();
            }
        }

        protected void Copy(ReadOnlySequence<byte> readableBuffer, PipeWriter writableBuffer)
        {
            if (readableBuffer.IsSingleSegment)
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

        protected virtual bool Read(ReadOnlySequence<byte> readableBuffer, PipeWriter writableBuffer, out SequencePosition consumed, out SequencePosition examined)
        {
            throw new NotImplementedException();
        }

        public static MessageBody For(
            HttpVersion httpVersion,
            HttpRequestHeaders headers,
            Http1Connection context)
        {
            // see also http://tools.ietf.org/html/rfc2616#section-4.4
            var keepAlive = httpVersion != HttpVersion.Http10;

            var upgrade = false;
            if (headers.HasConnection)
            {
                var connectionOptions = HttpHeaders.ParseConnection(headers.HeaderConnection);

                upgrade = (connectionOptions & ConnectionOptions.Upgrade) == ConnectionOptions.Upgrade;
                keepAlive = (connectionOptions & ConnectionOptions.KeepAlive) == ConnectionOptions.KeepAlive;
            }

            if (upgrade)
            {
                if (headers.HeaderTransferEncoding.Count > 0 || (headers.ContentLength.HasValue && headers.ContentLength.Value != 0))
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.UpgradeRequestCannotHavePayload);
                }

                return new ForUpgrade(context);
            }

            if (headers.HasTransferEncoding)
            {
                var transferEncoding = headers.HeaderTransferEncoding;
                var transferCoding = HttpHeaders.GetFinalTransferCoding(transferEncoding);

                // https://tools.ietf.org/html/rfc7230#section-3.3.3
                // If a Transfer-Encoding header field
                // is present in a request and the chunked transfer coding is not
                // the final encoding, the message body length cannot be determined
                // reliably; the server MUST respond with the 400 (Bad Request)
                // status code and then close the connection.
                if (transferCoding != TransferCoding.Chunked)
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.FinalTransferCodingNotChunked, in transferEncoding);
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

                return new ForContentLength(keepAlive, contentLength, context);
            }

            // If we got here, request contains no Content-Length or Transfer-Encoding header.
            // Reject with 411 Length Required.
            if (context.Method == HttpMethod.Post || context.Method == HttpMethod.Put)
            {
                var requestRejectionReason = httpVersion == HttpVersion.Http11 ? RequestRejectionReason.LengthRequired : RequestRejectionReason.LengthRequiredHttp10;
                BadHttpRequestException.Throw(requestRejectionReason, context.Method);
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

            public override bool IsEmpty => true;

            protected override bool Read(ReadOnlySequence<byte> readableBuffer, PipeWriter writableBuffer, out SequencePosition consumed, out SequencePosition examined)
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

            protected override bool Read(ReadOnlySequence<byte> readableBuffer, PipeWriter writableBuffer, out SequencePosition consumed, out SequencePosition examined)
            {
                if (_inputLength == 0)
                {
                    throw new InvalidOperationException("Attempted to read from completed Content-Length request body.");
                }

                var actual = (int)Math.Min(readableBuffer.Length, _inputLength);
                _inputLength -= actual;

                consumed = readableBuffer.GetPosition(actual);
                examined = consumed;

                Copy(readableBuffer.Slice(0, actual), writableBuffer);

                return _inputLength == 0;
            }

            protected override void OnReadStarting()
            {
                if (_contentLength > _context.MaxRequestBodySize)
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge);
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

            private Mode _mode = Mode.Prefix;

            public ForChunkedEncoding(bool keepAlive, Http1Connection context)
                : base(context)
            {
                RequestKeepAlive = keepAlive;
            }

            protected override bool Read(ReadOnlySequence<byte> readableBuffer, PipeWriter writableBuffer, out SequencePosition consumed, out SequencePosition examined)
            {
                consumed = default(SequencePosition);
                examined = default(SequencePosition);

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

                // _consumedBytes aren't tracked for trailer headers, since headers have separate limits.
                if (_mode == Mode.TrailerHeaders)
                {
                    if (_context.TakeMessageHeaders(readableBuffer, out consumed, out examined))
                    {
                        _mode = Mode.Complete;
                    }
                }

                return _mode == Mode.Complete;
            }

            private void ParseChunkedPrefix(ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
            {
                consumed = buffer.Start;
                examined = buffer.Start;
                var reader = new BufferReader(buffer);
                var ch1 = reader.Read();
                var ch2 = reader.Read();

                if (ch1 == -1 || ch2 == -1)
                {
                    examined = reader.Position;
                    return;
                }

                var chunkSize = CalculateChunkSize(ch1, 0);
                ch1 = ch2;

                while (reader.ConsumedBytes < MaxChunkPrefixBytes)
                {
                    if (ch1 == ';')
                    {
                        consumed = reader.Position;
                        examined = reader.Position;

                        AddAndCheckConsumedBytes(reader.ConsumedBytes);
                        _inputLength = chunkSize;
                        _mode = Mode.Extension;
                        return;
                    }

                    ch2 = reader.Read();
                    if (ch2 == -1)
                    {
                        examined = reader.Position;
                        return;
                    }

                    if (ch1 == '\r' && ch2 == '\n')
                    {
                        consumed = reader.Position;
                        examined = reader.Position;

                        AddAndCheckConsumedBytes(reader.ConsumedBytes);
                        _inputLength = chunkSize;
                        _mode = chunkSize > 0 ? Mode.Data : Mode.Trailer;
                        return;
                    }

                    chunkSize = CalculateChunkSize(ch1, chunkSize);
                    ch1 = ch2;
                }

                // At this point, 10 bytes have been consumed which is enough to parse the max value "7FFFFFFF\r\n".
                BadHttpRequestException.Throw(RequestRejectionReason.BadChunkSizeData);
            }

            private void ParseExtension(ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
            {
                // Chunk-extensions not currently parsed
                // Just drain the data
                consumed = buffer.Start;
                examined = buffer.Start;

                do
                {
                    SequencePosition? extensionCursorPosition = buffer.PositionOf(ByteCR);
                    if (extensionCursorPosition == null)
                    {
                        // End marker not found yet
                        consumed = buffer.End;
                        examined = buffer.End;
                        AddAndCheckConsumedBytes(buffer.Length);
                        return;
                    };

                    var extensionCursor = extensionCursorPosition.Value;
                    var charsToByteCRExclusive = buffer.Slice(0, extensionCursor).Length;

                    var suffixBuffer = buffer.Slice(extensionCursor);
                    if (suffixBuffer.Length < 2)
                    {
                        consumed = extensionCursor;
                        examined = buffer.End;
                        AddAndCheckConsumedBytes(charsToByteCRExclusive);
                        return;
                    }

                    suffixBuffer = suffixBuffer.Slice(0, 2);
                    var suffixSpan = suffixBuffer.ToSpan();

                    if (suffixSpan[1] == '\n')
                    {
                        // We consumed the \r\n at the end of the extension, so switch modes.
                        _mode = _inputLength > 0 ? Mode.Data : Mode.Trailer;

                        consumed = suffixBuffer.End;
                        examined = suffixBuffer.End;
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

            private void ReadChunkedData(ReadOnlySequence<byte> buffer, PipeWriter writableBuffer, out SequencePosition consumed, out SequencePosition examined)
            {
                var actual = Math.Min(buffer.Length, _inputLength);
                consumed = buffer.GetPosition(actual);
                examined = consumed;

                Copy(buffer.Slice(0, actual), writableBuffer);

                _inputLength -= actual;
                AddAndCheckConsumedBytes(actual);

                if (_inputLength == 0)
                {
                    _mode = Mode.Suffix;
                }
            }

            private void ParseChunkedSuffix(ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
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
                    BadHttpRequestException.Throw(RequestRejectionReason.BadChunkSuffix);
                }
            }

            private void ParseChunkedTrailer(ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
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

                BadHttpRequestException.Throw(RequestRejectionReason.BadChunkSizeData);
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
