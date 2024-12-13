// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

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
    private Task? _pumpTask;
    private readonly Pipe _requestBodyPipe;
    private ReadResult _readResult;

    public Http1ChunkedEncodingMessageBody(Http1Connection context, bool keepAlive)
        : base(context, keepAlive)
    {
        _requestBodyPipe = CreateRequestBodyPipe(context);
    }

    public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
    {
        TrackConsumedAndExaminedBytes(_readResult, consumed, examined);
        _requestBodyPipe.Reader.AdvanceTo(consumed, examined);
    }

    public override bool TryReadInternal(out ReadResult readResult)
    {
        TryStartAsync();

        var boolResult = _requestBodyPipe.Reader.TryRead(out _readResult);

        readResult = _readResult;
        CountBytesRead(readResult.Buffer.Length);

        if (_readResult.IsCompleted)
        {
            TryStop();
        }

        return boolResult;
    }

    public override async ValueTask<ReadResult> ReadAsyncInternal(CancellationToken cancellationToken = default)
    {
        await TryStartAsync();

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

    public override void CancelPendingRead()
    {
        _requestBodyPipe.Reader.CancelPendingRead();
    }

    private async Task PumpAsync()
    {
        Debug.Assert(!RequestUpgrade, "Upgraded connections should never use this code path!");

        Exception? error = null;

        try
        {
            var awaitable = _context.Input.ReadAsync();

            if (!awaitable.IsCompleted)
            {
                await TryProduceContinueAsync();
            }

            while (true)
            {
                var result = await awaitable;

                if (_context.RequestTimedOut)
                {
                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTimeout);
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
                        KestrelMetrics.AddConnectionEndReason(_context.MetricsContext, ConnectionEndReason.UnexpectedEndOfRequestContent);
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
            await _requestBodyPipe.Writer.CompleteAsync(error);
        }
    }

    protected override ValueTask OnStopAsync()
    {
        if (!_context.HasStartedConsumingRequestBody)
        {
            return default;
        }

        // call complete here on the reader
        _requestBodyPipe.Reader.Complete();

        Debug.Assert(_pumpTask != null, "OnReadStartedAsync must have been called.");

        // PumpTask catches all Exceptions internally.
        if (_pumpTask.IsCompleted)
        {
            // At this point both the request body pipe reader and writer should be completed.
            _requestBodyPipe.Reset();
            return default;
        }

        // Should I call complete here?
        return StopAsyncAwaited(_pumpTask);
    }

    private async ValueTask StopAsyncAwaited(Task pumpTask)
    {
        _canceled = true;
        _context.Input.CancelPendingRead();
        await pumpTask;

        // At this point both the request body pipe reader and writer should be completed.
        _requestBodyPipe.Reset();
    }

    protected override Task OnReadStartedAsync()
    {
        _pumpTask = PumpAsync();
        return Task.CompletedTask;
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
            var reader = new SequenceReader<byte>(readableBuffer);
            if (_context.TakeMessageHeaders(ref reader, trailers: true))
            {
                examined = reader.Position;
                _mode = Mode.Complete;
            }
            else
            {
                examined = readableBuffer.End;
            }

            consumed = reader.Position;
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

                AddAndCheckObservedBytes(reader.Consumed);
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

                AddAndCheckObservedBytes(reader.Consumed);
                _inputLength = chunkSize;
                _mode = chunkSize > 0 ? Mode.Data : Mode.Trailer;
                return;
            }

            chunkSize = CalculateChunkSize(ch1, chunkSize);
            ch1 = ch2;
        }

        // At this point, 10 bytes have been consumed which is enough to parse the max value "7FFFFFFF\r\n".
        KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkSizeData);
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
                AddAndCheckObservedBytes(buffer.Length);
                return;
            };

            var extensionCursor = extensionCursorPosition.Value;
            var charsToByteCRExclusive = buffer.Slice(0, extensionCursor).Length;

            var suffixBuffer = buffer.Slice(extensionCursor);
            if (suffixBuffer.Length < 2)
            {
                consumed = extensionCursor;
                examined = buffer.End;
                AddAndCheckObservedBytes(charsToByteCRExclusive);
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
                AddAndCheckObservedBytes(charsToByteCRExclusive + 2);
            }
            else
            {
                // Don't consume suffixSpan[1] in case it is also a \r.
                buffer = buffer.Slice(charsToByteCRExclusive + 1);
                consumed = extensionCursor;
                AddAndCheckObservedBytes(charsToByteCRExclusive + 1);
            }
        } while (_mode == Mode.Extension);
    }

    private void ReadChunkedData(in ReadOnlySequence<byte> buffer, PipeWriter writableBuffer, out SequencePosition consumed, out SequencePosition examined)
    {
        var actual = Math.Min(buffer.Length, _inputLength);
        consumed = buffer.GetPosition(actual);
        examined = consumed;

        buffer.Slice(0, actual).CopyTo(writableBuffer);

        _inputLength -= actual;
        AddAndCheckObservedBytes(actual);

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
            AddAndCheckObservedBytes(2);
            _mode = Mode.Prefix;
        }
        else
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkSuffix);
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
            AddAndCheckObservedBytes(2);
            _mode = Mode.Complete;
            // No trailers
            _context.OnTrailersComplete();
        }
        else
        {
            _mode = Mode.TrailerHeaders;
        }
    }

    private static int CalculateChunkSize(int extraHexDigit, int currentParsedSize)
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

        KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkSizeData);

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

    private static Pipe CreateRequestBodyPipe(Http1Connection context)
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
