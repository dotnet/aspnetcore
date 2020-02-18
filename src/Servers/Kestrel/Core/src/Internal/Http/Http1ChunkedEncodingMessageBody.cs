// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    /// <summary>
    ///   http://tools.ietf.org/html/rfc2616#section-3.6.1
    /// </summary>
    internal sealed class Http1ChunkedEncodingMessageBody : Http1MessageBody
    {
        // byte consts don't have a data type annotation so we pre-cast it
        private const byte ByteCR = (byte)'\r';
        // "7FFFFFFF\r\n" is the largest chunk size that could be returned as an int.
        private const int MaxChunkPrefixBytes = 10;

        private long _inputLength;

        private Mode _mode = Mode.Prefix;
        private volatile bool _canceled;
        private Task _pumpTask;
        private readonly Pipe _requestBodyPipe;
        private ReadResult _readResult;

        public Http1ChunkedEncodingMessageBody(bool keepAlive, Http1Connection context)
            : base(context)
        {
            RequestKeepAlive = keepAlive;
            _requestBodyPipe = CreateRequestBodyPipe(context);
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceTo(consumed, consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            OnAdvance(_readResult, consumed, examined);
            _requestBodyPipe.Reader.AdvanceTo(consumed, examined);
        }

        public override bool TryRead(out ReadResult readResult)
        {
            ThrowIfCompleted();

            return TryReadInternal(out readResult);
        }

        public override bool TryReadInternal(out ReadResult readResult)
        {
            TryStart();

            var boolResult = _requestBodyPipe.Reader.TryRead(out _readResult);

            readResult = _readResult;
            CountBytesRead(readResult.Buffer.Length);

            if (_readResult.IsCompleted)
            {
                TryStop();
            }

            return boolResult;
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfCompleted();
            return ReadAsyncInternal(cancellationToken);
        }

        public override async ValueTask<ReadResult> ReadAsyncInternal(CancellationToken cancellationToken = default)
        {
            TryStart();

            try
            {
                var readAwaitable = _requestBodyPipe.Reader.ReadAsync(cancellationToken);

                _readResult = await StartTimingReadAsync(readAwaitable, cancellationToken);
            }
            catch (ConnectionAbortedException ex)
            {
                throw new TaskCanceledException("The request was aborted", ex);
            }

            StopTimingRead(_readResult.Buffer.Length);

            if (_readResult.IsCompleted)
            {
                TryStop();
            }

            return _readResult;
        }

        public override void Complete(Exception exception)
        {
            _completed = true;
            _context.ReportApplicationError(exception);
        }

        public override void CancelPendingRead()
        {
            _requestBodyPipe.Reader.CancelPendingRead();
        }

        private async Task PumpAsync()
        {
            Debug.Assert(!RequestUpgrade, "Upgraded connections should never use this code path!");

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
                            done = Read(readableBuffer, _requestBodyPipe.Writer, out consumed, out examined);

                            await _requestBodyPipe.Writer.FlushAsync();

                            if (done)
                            {
                                break;
                            }
                        }

                        // Read() will have already have greedily consumed the entire request body if able.
                        if (result.IsCompleted)
                        {
                            ThrowUnexpectedEndOfRequestContent();
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
                _requestBodyPipe.Writer.Complete(error);
            }
        }

        protected override Task OnStopAsync()
        {
            if (!_context.HasStartedConsumingRequestBody)
            {
                return Task.CompletedTask;
            }

            // call complete here on the reader
            _requestBodyPipe.Reader.Complete();

            // PumpTask catches all Exceptions internally.
            if (_pumpTask.IsCompleted)
            {
                // At this point both the request body pipe reader and writer should be completed.
                _requestBodyPipe.Reset();
                return Task.CompletedTask;
            }

            // Should I call complete here?
            return StopAsyncAwaited();
        }

        private async Task StopAsyncAwaited()
        {
            _canceled = true;
            _context.Input.CancelPendingRead();
            await _pumpTask;

            // At this point both the request body pipe reader and writer should be completed.
            _requestBodyPipe.Reset();
        }

        private void Copy(in ReadOnlySequence<byte> readableBuffer, PipeWriter writableBuffer)
        {
            if (readableBuffer.IsSingleSegment)
            {
                writableBuffer.Write(readableBuffer.FirstSpan);
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

        private bool Read(ReadOnlySequence<byte> readableBuffer, PipeWriter writableBuffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = default;
            examined = default;

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
                if (_context.TakeMessageHeaders(readableBuffer, trailers: true, out consumed, out examined))
                {
                    _mode = Mode.Complete;
                }
            }

            return _mode == Mode.Complete;
        }

        private void ParseChunkedPrefix(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;
            var reader = new SequenceReader<byte>(buffer);

            if (!reader.TryRead(out var ch1) || !reader.TryRead(out var ch2))
            {
                examined = reader.Position;
                return;
            }

            // Advance examined before possibly throwing, so we don't risk examining less than the previous call to ParseChunkedPrefix.
            examined = reader.Position;

            var chunkSize = CalculateChunkSize(ch1, 0);
            ch1 = ch2;

            while (reader.Consumed < MaxChunkPrefixBytes)
            {
                if (ch1 == ';')
                {
                    consumed = reader.Position;
                    examined = reader.Position;

                    AddAndCheckConsumedBytes(reader.Consumed);
                    _inputLength = chunkSize;
                    _mode = Mode.Extension;
                    return;
                }

                if (!reader.TryRead(out ch2))
                {
                    examined = reader.Position;
                    return;
                }

                // Advance examined before possibly throwing, so we don't risk examining less than the previous call to ParseChunkedPrefix.
                examined = reader.Position;

                if (ch1 == '\r' && ch2 == '\n')
                {
                    consumed = reader.Position;

                    AddAndCheckConsumedBytes(reader.Consumed);
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

        private void ReadChunkedData(in ReadOnlySequence<byte> buffer, PipeWriter writableBuffer, out SequencePosition consumed, out SequencePosition examined)
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

        private void ParseChunkedSuffix(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;

            if (buffer.Length < 2)
            {
                examined = buffer.End;
                return;
            }

            var suffixBuffer = buffer.Slice(0, 2);
            var suffixSpan = suffixBuffer.ToSpan();

            // Advance examined before possibly throwing, so we don't risk examining less than the previous call to ParseChunkedSuffix.
            examined = suffixBuffer.End;

            if (suffixSpan[0] == '\r' && suffixSpan[1] == '\n')
            {
                consumed = suffixBuffer.End;
                AddAndCheckConsumedBytes(2);
                _mode = Mode.Prefix;
            }
            else
            {
                BadHttpRequestException.Throw(RequestRejectionReason.BadChunkSuffix);
            }
        }

        private void ParseChunkedTrailer(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;

            if (buffer.Length < 2)
            {
                examined = buffer.End;
                return;
            }

            var trailerBuffer = buffer.Slice(0, 2);
            var trailerSpan = trailerBuffer.ToSpan();

            // Advance examined before possibly throwing, so we don't risk examining less than the previous call to ParseChunkedTrailer.
            examined = trailerBuffer.End;

            if (trailerSpan[0] == '\r' && trailerSpan[1] == '\n')
            {
                consumed = trailerBuffer.End;
                AddAndCheckConsumedBytes(2);
                _mode = Mode.Complete;
                // No trailers
                _context.OnTrailersComplete();
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

        private Pipe CreateRequestBodyPipe(Http1Connection context)
            => new Pipe(new PipeOptions
            (
                pool: context.MemoryPool,
                readerScheduler: context.ServiceContext.Scheduler,
                writerScheduler: PipeScheduler.Inline,
                pauseWriterThreshold: 1,
                resumeWriterThreshold: 1,
                useSynchronizationContext: false,
                minimumSegmentSize: context.MemoryPool.GetMinimumSegmentSize()
            ));
    }
}
