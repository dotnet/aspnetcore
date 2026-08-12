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
    private const byte ByteEqual = (byte)'=';

    // "7FFFFFFF" is the largest chunk size that could be returned as an int.
    private const int MaxChunkPrefixBytes = 8;

    // https://www.rfc-editor.org/info/rfc9110/#section-5.6.2
    // tchar          = "!" / "#" / "$" / "%" / "&" / "'" / "*"
    //                / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
    //                / DIGIT / ALPHA
    //                ; any VCHAR, except delimiters
    private static ReadOnlySpan<byte> s_tchar => "!#$%&'*+-.^_`|~0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"u8;

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
                ParseChunkedPrefixIncludingCRLF(readableBuffer, out consumed, out examined);

                if (_mode == Mode.Prefix)
                {
                    return false;
                }

                readableBuffer = readableBuffer.Slice(consumed);
            }

            if (_mode == Mode.Extension)
            {
                ParseExtensionIncludingCRLF(readableBuffer, out consumed, out examined);

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

    private void ParseChunkedPrefixIncludingCRLF(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
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

            // Advance examined before possibly throwing, so we don't risk examining less than the previous call to ParseChunkedPrefix.
            examined = reader.Position;

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

    private static bool TryEatByte(ref SequenceReader<byte> reader, Func<byte, bool> predicate)
    {
        var span = reader.UnreadSpan;
        if (span.IsEmpty)
        {
            return false;
        }

        if (predicate(span[0]))
        {
            reader.Advance(1);
            return true;
        }

        return false;
    }

    private static bool IsBadWhitespaceByte(byte b)
        => b is 0x20 or 0x09;

    private static bool EatBadWhitespace(ref SequenceReader<byte> reader)
    {
        // https://www.rfc-editor.org/info/rfc9110#section-5.6.3
        // BWS            = OWS
        //                ; "bad" whitespace
        //
        // OWS = *(SP / HTAB)
        //     ; optional whitespace
        return reader.AdvancePastAny([0x20, 0x09]) > 0;
    }

    private static void EatChunkExtensionName(ref SequenceReader<byte> reader)
    {
        // https://www.rfc-editor.org/info/rfc9112/#section-7.1.1
        // chunk-ext-name = token
        //
        // https://www.rfc-editor.org/info/rfc9110/#section-5.6.2
        // token          = 1*tchar
        var bytesRead = reader.AdvancePastAny(s_tchar);
        if (bytesRead == 0)
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
        }
    }

    private static bool EatChunkExtensionValue(ref SequenceReader<byte> reader)
    {
        // https://www.rfc-editor.org/info/rfc9112/#section-7.1.1
        // chunk-ext-val  = token / quoted-string
        //
        // https://www.rfc-editor.org/info/rfc9110#section-5.6.4
        // quoted-string  = DQUOTE *( qdtext / quoted-pair ) DQUOTE
        if (!reader.IsNext((byte)'"'))
        {
            // We have 'token', not 'quoted-string'.
            var bytesRead = reader.AdvancePastAny(s_tchar);
            if (bytesRead == 0)
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
            }

            return true;
        }

        // We have 'quoted-string'.
        // We advance over the opening DQUOTE.
        reader.Advance(1);

        // We keep eating qdtext / quoted-pair until we
        // have no more data, or we find the closing DQUOTE.
        while (!reader.End && !reader.IsNext((byte)'"'))
        {
            if (!EatQdtextOrQuotedPair(ref reader))
            {
                // A quoted-pair was started but no more data is available to
                // complete it.
                return false;
            }
        }

        if (reader.End)
        {
            return false;
        }

        // Advance over the closing DQUOTE.
        reader.Advance(1);
        return true;
    }

    private static bool EatQdtextOrQuotedPair(ref SequenceReader<byte> reader)
    {
        // https://www.rfc-editor.org/info/rfc9110#section-5.6.4
        // qdtext         = HTAB / SP / %x21 / %x23-5B / %x5D-7E / obs-text
        //
        // quoted-pair    = "\" ( HTAB / SP / VCHAR / obs-text )
        //
        // obs-text = %x80-FF
        if (reader.IsNext((byte)'\\'))
        {
            reader.Advance(1);

            if (reader.End)
            {
                return false;
            }

            if (!TryEatByte(
                ref reader,
                // https://www.rfc-editor.org/info/rfc5234/#appendix-B.1
                // HTAB           =  %x09
                // SP             =  %x20
                // VCHAR          =  %x21-7E
                predicate: static b => b == 0x09 || b == 0x20 || (b >= 0x21 && b <= 0x7E) || (b >= 0x80 && b <= 0xFF)))
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
            }

            return true;
        }

        if (!TryEatByte(
            ref reader,
            predicate: static b => b == 0x09 || b == 0x20 || b == 0x21 || (b >= 0x23 && b <= 0x5B) || (b >= 0x5D && b <= 0x7E) || (b >= 0x80 && b <= 0xFF)
            ))
        {
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
        }

        return true;
    }

    // https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1
    // chunk-ext      = *( BWS ";" BWS chunk-ext-name
    //                     [BWS "=" BWS chunk-ext-val] )
    private void ParseExtensionIncludingCRLF(ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
    {
        var reader = new SequenceReader<byte>(buffer);
        consumed = reader.Position;

        while (true)
        {
            EatBadWhitespace(ref reader);
            examined = reader.Position;

            if (!reader.TryRead(out var expectedSemicolon))
            {
                return;
            }

            examined = reader.Position;

            if (expectedSemicolon != ByteSemicolon)
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
            }

            EatBadWhitespace(ref reader);
            examined = reader.Position;

            if (reader.End)
            {
                return;
            }

            EatChunkExtensionName(ref reader);
            examined = reader.Position;

            var hasBWSAfterExtensionName = EatBadWhitespace(ref reader);
            examined = reader.Position;

            if (!reader.TryPeek(out var afterExtensionNameAndBWS))
            {
                return;
            }

            // If we found BWS after the extension name, then
            // we must have either an "=" or a next extension starting with a ";"
            // If we have a ";", continue parsing the next extension.
            if (afterExtensionNameAndBWS == ByteSemicolon)
            {
                // Note that we didn't consume the ";" here. The next iteration will do.
                consumed = reader.Position;
                examined = reader.Position;
                AddAndCheckObservedBytes(reader.Consumed);

                // Resets reader.Consumed to 0, so we can continue parsing the next extension and report
                // the number of observed bytes correctly for each part.
                reader = new SequenceReader<byte>(reader.UnreadSequence);
                continue;
            }

            if (afterExtensionNameAndBWS == ByteEqual)
            {
                // Advance over the "=".
                reader.Advance(1);
                EatBadWhitespace(ref reader);
                examined = reader.Position;

                if (reader.End)
                {
                    // We can't make the next decision, so we return.
                    return;
                }

                if (!EatChunkExtensionValue(ref reader))
                {
                    // EatChunkExtensionValue returns false when it's waiting for closing
                    // double quote but no more data is available. In this case, we can't
                    // make the next decision, so we mark the examined data and return.
                    examined = reader.Position;
                    return;
                }

                examined = reader.Position;
            }

            // If we had whitespace after the extension name, then we must have an "=" after it.
            // Note that the case for ";" was handled above.
            if (hasBWSAfterExtensionName && afterExtensionNameAndBWS != ByteEqual)
            {
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.BadChunkExtension);
            }

            if (reader.Remaining < 2)
            {
                // We can't make the next decision if we don't have at least 2 bytes to read.
                // However, if we have one character (e.g, potentially CR), we want to mark it as
                // examined.
                _ = reader.TryRead(out _);
                examined = reader.Position;
                return;
            }

            if (reader.IsNext([ByteCR, ByteLF]))
            {
                reader.Advance(2);
                _mode = _inputLength > 0 ? Mode.Data : Mode.Trailer;
                AddAndCheckObservedBytes(reader.Consumed);
                consumed = reader.Position;
                examined = reader.Position;
                return;
            }

            // If we parsed the extension value, and we still have more data which is not CRLF, that must belong to the next extension.
            // We can safely mark the current position as consumed.
            consumed = reader.Position;
            AddAndCheckObservedBytes(reader.Consumed);

            // Resets reader.Consumed to 0, so we can continue parsing the next extension and report
            // the number of observed bytes correctly for each part.
            reader = new SequenceReader<byte>(reader.UnreadSequence);
        }
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
