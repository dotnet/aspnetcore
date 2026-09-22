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
    private const byte ByteLF = (byte)'\n';
    private const byte ByteSemicolon = (byte)';';

    // "7FFFFFFF" is the largest chunk size that could be returned as an int.
    private const int MaxChunkPrefixBytes = 8;

    private long _inputLength;

    private Mode _mode = Mode.Prefix;
    private volatile bool _canceled;
    private Task? _pumpTask;
    private readonly Pipe _requestBodyPipe;
    private ReadResult _readResult;

    private ChunkedExtensionParser? _chunkedExtensionParser;

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

        // https://www.rfc-editor.org/rfc/rfc9112#section-7.1
        // chunked-body   = *chunk
        //                  last-chunk
        //                  trailer-section
        //                  CRLF
        //
        // chunk          = chunk-size [ chunk-ext ] CRLF
        //                  chunk-data CRLF
        //
        // chunk-size     = 1*HEXDIG
        //
        // last-chunk     = 1*("0") [ chunk-ext ] CRLF
        //
        // chunk-data     = 1*OCTET
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

        if (!reader.TryRead(out var ch))
        {
            examined = reader.Position;
            return;
        }

        // Advance examined before possibly throwing, so we don't risk examining less than the previous call to ParseChunkedPrefix.
        examined = reader.Position;

        var chunkSize = CalculateChunkSize(ch, 0);

        while (reader.Consumed <= MaxChunkPrefixBytes)
        {
            // We only peek here.
            // If this was a semicolon or BWS, we don't want to advance
            // the reader, because we want the extension parsing to
            // consume that byte, not here.
            if (!reader.TryPeek(out ch))
            {
                return;
            }

            // An extension can only start with either semicolon or BWS.
            // And data or trailer can never start with semicolon nor BWS.
            if (ch == ByteSemicolon || IsBadWhitespaceByte(ch))
            {
                examined = reader.Position;
                consumed = reader.Position;

                AddAndCheckObservedBytes(reader.Consumed);
                _inputLength = chunkSize;
                _mode = Mode.Extension;

                var reject = !_context.ServiceContext.ServerOptions.EnableChunkedExtensions;

                _context.OnChunkedExtension(reject);

                if (reject)
                {
                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.ChunkedExtensionNotAllowed);
                }

                return;
            }

            reader.Advance(1);
            examined = reader.Position;

            if (ch == ByteCR)
            {
                // We have CR but we don't know yet what will be after it.
                if (!reader.TryRead(out var expectedLF))
                {
                    return;
                }

                examined = reader.Position;

                if (expectedLF != ByteLF)
                {
                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkSizeData);
                }

                consumed = reader.Position;

                AddAndCheckObservedBytes(reader.Consumed);
                _inputLength = chunkSize;
                _mode = chunkSize > 0 ? Mode.Data : Mode.Trailer;
                return;
            }

            if (reader.Consumed > MaxChunkPrefixBytes)
            {
                // We consumed already the 8 bytes fully. And the next byte wasn't CRLF nor semicolon.
                break;
            }

            chunkSize = CalculateChunkSize(ch, chunkSize);
        }

        // At this point, 8 bytes have been consumed which is enough to parse the max value "7FFFFFFF".
        KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkSizeData);
    }

    private static bool IsBadWhitespaceByte(byte b)
        => b is 0x20 or 0x09;

    private void ParseExtension(ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
    {
        var parser = _chunkedExtensionParser ?? new ChunkedExtensionParser();

        var reader = new SequenceReader<byte>(buffer);
        if (parser.Consume(ref reader, out consumed, out examined))
        {
            _mode = _inputLength > 0 ? Mode.Data : Mode.Trailer;

            // If the next chunk has an extension, we will create a new parser with fresh state.
            // Alternatively, we could also reuse the same parser, but we will want to remove
            // the "Completed" state in ChunkedExtensionParser and use StartOfExtension instead.
            _chunkedExtensionParser = null;
        }
        else
        {
            // ChunkedExtensionParser is a struct.
            // We want to ensure that we don't lose the state of the parser across multiple calls to ParseExtension.
            // We ensure we have the state in the private field, and not just in a "copy" of the struct.
            _chunkedExtensionParser = parser;
        }

        AddAndCheckObservedBytes(reader.Consumed);
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
