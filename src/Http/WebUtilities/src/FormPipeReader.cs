// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// Used to read an 'application/x-www-form-urlencoded' form.
/// Internally reads from a PipeReader.
/// </summary>
public class FormPipeReader
{
    private const int StackAllocThreshold = 128;
    private const int DefaultValueCountLimit = 1024;
    private const int DefaultKeyLengthLimit = 1024 * 2;
    private const int DefaultValueLengthLimit = 1024 * 1024 * 4;

    // Used for UTF8/ASCII (precalculated for fast path)
    // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
    private static ReadOnlySpan<byte> UTF8EqualEncoded => "="u8;
    private static ReadOnlySpan<byte> UTF8AndEncoded => "&"u8;

    // Used for other encodings
    private readonly byte[]? _otherEqualEncoding;
    private readonly byte[]? _otherAndEncoding;

    private readonly PipeReader _pipeReader;
    private readonly Encoding _encoding;

    /// <summary>
    /// Initializes a new instance of <see cref="FormPipeReader"/>.
    /// </summary>
    /// <param name="pipeReader">The <see cref="PipeReader"/> to read from.</param>
    public FormPipeReader(PipeReader pipeReader)
        : this(pipeReader, Encoding.UTF8)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FormPipeReader"/>.
    /// </summary>
    /// <param name="pipeReader">The <see cref="PipeReader"/> to read from.</param>
    /// <param name="encoding">The <see cref="Encoding"/>.</param>
    public FormPipeReader(PipeReader pipeReader, Encoding encoding)
    {
        // https://learn.microsoft.com/en-us/dotnet/core/compatibility/syslib-warnings/syslib0001
        if (encoding is Encoding { CodePage: 65000 })
        {
            throw new ArgumentException("UTF7 is unsupported and insecure. Please select a different encoding.");
        }

        _pipeReader = pipeReader;
        _encoding = encoding;

        if (_encoding != Encoding.UTF8 && _encoding != Encoding.ASCII)
        {
            _otherEqualEncoding = _encoding.GetBytes("=");
            _otherAndEncoding = _encoding.GetBytes("&");
        }
    }

    /// <summary>
    /// The limit on the number of form values to allow in ReadForm or ReadFormAsync.
    /// </summary>
    public int ValueCountLimit { get; set; } = DefaultValueCountLimit;

    /// <summary>
    /// The limit on the length of form keys.
    /// </summary>
    public int KeyLengthLimit { get; set; } = DefaultKeyLengthLimit;

    /// <summary>
    /// The limit on the length of form values.
    /// </summary>
    public int ValueLengthLimit { get; set; } = DefaultValueLengthLimit;

    /// <summary>
    /// Parses an HTTP form body.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The collection containing the parsed HTTP form body.</returns>
    public async Task<Dictionary<string, StringValues>> ReadFormAsync(CancellationToken cancellationToken = default)
    {
        KeyValueAccumulator accumulator = default;
        long scannedLength = 0;
        while (true)
        {
            var readResult = await _pipeReader.ReadAsync(cancellationToken);

            var buffer = readResult.Buffer;

            if (!buffer.IsEmpty)
            {
                try
                {
                    ParseFormValues(ref buffer, ref accumulator, ref scannedLength, readResult.IsCompleted);
                }
                catch
                {
                    _pipeReader.AdvanceTo(buffer.Start, buffer.End);
                    throw;
                }
            }

            if (readResult.IsCompleted)
            {
                _pipeReader.AdvanceTo(buffer.End);

                if (!buffer.IsEmpty)
                {
                    throw new InvalidOperationException("End of body before form was fully parsed.");
                }
                break;
            }

            _pipeReader.AdvanceTo(buffer.Start, buffer.End);
        }

        return accumulator.GetResults();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void ParseFormValues(
        ref ReadOnlySequence<byte> buffer,
        ref KeyValueAccumulator accumulator,
        ref long scannedLength,
        bool isFinalBlock)
    {
        if (buffer.IsSingleSegment)
        {
            ParseFormValuesFast(buffer.FirstSpan, ref accumulator, ref scannedLength, isFinalBlock, out var consumed);

            buffer = buffer.Slice(consumed);
            return;
        }

        ParseValuesSlow(ref buffer, ref accumulator, ref scannedLength, isFinalBlock);
    }

    // Fast parsing for single span in ReadOnlySequence
    private void ParseFormValuesFast(ReadOnlySpan<byte> span,
        ref KeyValueAccumulator accumulator,
        ref long scannedLength,
        bool isFinalBlock,
        out int consumed)
    {
        ReadOnlySpan<byte> key;
        ReadOnlySpan<byte> value;
        consumed = 0;
        var equalsDelimiter = GetEqualsForEncoding();
        var andDelimiter = GetAndForEncoding();

        // The delimiter can span multiple bytes (e.g. UTF-16/UTF-32), so it may straddle the end of a read.
        // 'scannedLength' therefore never covers the last delimiterOverlap bytes, which have to be rescanned.
        var delimiterOverlap = andDelimiter.Length - 1;

        while (span.Length > 0)
        {
            // Find the end of the key=value pair.
            // Start the search from the scannedLength to avoid rescanning bytes we already scanned
            var searchStart = (int)Math.Min(scannedLength, span.Length);
            var ampersandRelative = span[searchStart..].IndexOf(andDelimiter);
            var ampersand = ampersandRelative == -1 ? -1 : ampersandRelative + searchStart;
            ReadOnlySpan<byte> keyValuePair;
            int equals;
            var foundAmpersand = ampersand != -1;

            if (foundAmpersand)
            {
                keyValuePair = span.Slice(0, ampersand);
                span = span.Slice(keyValuePair.Length + andDelimiter.Length);
                consumed += keyValuePair.Length + andDelimiter.Length;
                // Reset: the new span starts fresh, nothing pre-scanned.
                scannedLength = 0;
            }
            else
            {
                // We can't know that what is currently read is the end of the form value, that's only the case if this is the final block
                // If we're not in the final block, then consume nothing
                if (!isFinalBlock)
                {
                    // Don't buffer indefinitely
                    if ((uint)span.Length > (uint)KeyLengthLimit + (uint)ValueLengthLimit)
                    {
                        ThrowKeyOrValueTooLargeException();
                    }
                    // Record how far we've scanned so the next call skips these bytes. The trailing
                    // delimiterOverlap bytes are left out: a multi-byte delimiter could start there and
                    // only be completed by the bytes that arrive next.
                    scannedLength = Math.Max(0, span.Length - delimiterOverlap);
                    return;
                }

                keyValuePair = span;
                span = default;
                consumed += keyValuePair.Length;
                scannedLength = 0;
            }

            equals = keyValuePair.IndexOf(equalsDelimiter);

            if (equals == -1)
            {
                // Too long for the whole segment to be a key.
                if (keyValuePair.Length > KeyLengthLimit)
                {
                    ThrowKeyTooLargeException();
                }

                // There is no more data, this segment must be "key" with no equals or value.
                key = keyValuePair;
                value = default;
            }
            else
            {
                key = keyValuePair.Slice(0, equals);
                if (key.Length > KeyLengthLimit)
                {
                    ThrowKeyTooLargeException();
                }

                value = keyValuePair.Slice(equals + equalsDelimiter.Length);
                if (value.Length > ValueLengthLimit)
                {
                    ThrowValueTooLargeException();
                }
            }

            var decodedKey = GetDecodedString(key);
            var decodedValue = GetDecodedString(value);

            AppendAndVerify(ref accumulator, decodedKey, decodedValue);
        }
    }

    // For multi-segment parsing of a read only sequence
    private void ParseValuesSlow(
        ref ReadOnlySequence<byte> buffer,
        ref KeyValueAccumulator accumulator,
        ref long scannedLength,
        bool isFinalBlock)
    {
        var sequenceReader = new SequenceReader<byte>(buffer);
        ReadOnlySequence<byte> keyValuePair;

        var consumed = sequenceReader.Position;
        var consumedBytes = default(long);
        var equalsDelimiter = GetEqualsForEncoding();
        var andDelimiter = GetAndForEncoding();

        // 'scannedLength' already excludes the trailing bytes a multi-byte delimiter could have started in,
        // so everything it covers can be skipped outright.
        var resumeSkipped = Math.Min(scannedLength, sequenceReader.Length);
        if (resumeSkipped > 0)
        {
            sequenceReader.Advance(resumeSkipped);
        }

        // Advancing can place the reader at the end when everything buffered was already scanned.
        // Those bytes are still unconsumed, so on the final block they must be parsed as the last pair.
        while (!sequenceReader.End || (isFinalBlock && consumedBytes < sequenceReader.Length))
        {
            if (!sequenceReader.TryReadTo(out keyValuePair, andDelimiter))
            {
                if (!isFinalBlock)
                {
                    // +2 to account for '&' and '='
                    if ((sequenceReader.Length - consumedBytes) > (long)KeyLengthLimit + (long)ValueLengthLimit + 2)
                    {
                        ThrowKeyOrValueTooLargeException();
                    }

                    // Record total bytes scanned from 'consumed' so next call skips them, minus the trailing
                    // bytes a multi-byte delimiter could have started in.
                    scannedLength = Math.Max(0, sequenceReader.Length - consumedBytes - (andDelimiter.Length - 1));
                    break;
                }

                // This must be the final key=value pair; take everything from the last consumed position.
                keyValuePair = buffer.Slice(consumed);
                sequenceReader.Advance(sequenceReader.Remaining);
            }
            else if (resumeSkipped > 0)
            {
                // TryReadTo only returned [resumeSkipped..'&'). Reconstruct the full pair
                // [consumed..'&') by prepending the already-scanned prefix.
                keyValuePair = buffer.Slice(consumed, resumeSkipped + keyValuePair.Length);
            }

            scannedLength = 0;
            resumeSkipped = 0;

            if (keyValuePair.IsSingleSegment)
            {
                var segmentScannedLength = 0L;
                ParseFormValuesFast(keyValuePair.FirstSpan, ref accumulator, ref segmentScannedLength, isFinalBlock: true, out var segmentConsumed);
                Debug.Assert(segmentConsumed == keyValuePair.FirstSpan.Length);
                consumedBytes = sequenceReader.Consumed;
                consumed = sequenceReader.Position;
                continue;
            }

            var keyValueReader = new SequenceReader<byte>(keyValuePair);
            ReadOnlySequence<byte> value;

            if (keyValueReader.TryReadTo(out ReadOnlySequence<byte> key, equalsDelimiter))
            {
                if (key.Length > KeyLengthLimit)
                {
                    ThrowKeyTooLargeException();
                }

                value = keyValuePair.Slice(keyValueReader.Position);
                if (value.Length > ValueLengthLimit)
                {
                    ThrowValueTooLargeException();
                }
            }
            else
            {
                // Too long for the whole segment to be a key.
                if (keyValuePair.Length > KeyLengthLimit)
                {
                    ThrowKeyTooLargeException();
                }

                // There is no more data, this segment must be "key" with no equals or value.
                key = keyValuePair;
                value = default;
            }

            var decodedKey = GetDecodedStringFromReadOnlySequence(key);
            var decodedValue = GetDecodedStringFromReadOnlySequence(value);

            AppendAndVerify(ref accumulator, decodedKey, decodedValue);

            consumedBytes = sequenceReader.Consumed;
            consumed = sequenceReader.Position;
        }

        buffer = buffer.Slice(consumed);
    }

    private void ThrowKeyOrValueTooLargeException()
    {
        throw new InvalidDataException(
            string.Format(
                CultureInfo.CurrentCulture,
                Resources.FormPipeReader_KeyOrValueTooLarge,
                KeyLengthLimit,
                ValueLengthLimit));
    }

    private void ThrowKeyTooLargeException()
    {
        throw new InvalidDataException(
            string.Format(
                CultureInfo.CurrentCulture,
                Resources.FormPipeReader_KeyTooLarge,
                KeyLengthLimit));
    }

    private void ThrowValueTooLargeException()
    {
        throw new InvalidDataException(
            string.Format(
                CultureInfo.CurrentCulture,
                Resources.FormPipeReader_ValueTooLarge,
                ValueLengthLimit));
    }

    [SkipLocalsInit]
    private string GetDecodedStringFromReadOnlySequence(in ReadOnlySequence<byte> ros)
    {
        if (ros.IsSingleSegment)
        {
            return GetDecodedString(ros.FirstSpan);
        }

        if (ros.Length < StackAllocThreshold)
        {
            Span<byte> buffer = stackalloc byte[StackAllocThreshold].Slice(0, (int)ros.Length);
            ros.CopyTo(buffer);
            return GetDecodedString(buffer);
        }
        else
        {
            var byteArray = ArrayPool<byte>.Shared.Rent((int)ros.Length);

            try
            {
                Span<byte> buffer = byteArray.AsSpan(0, (int)ros.Length);
                ros.CopyTo(buffer);
                return GetDecodedString(buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteArray);
            }
        }
    }

    // Check that key/value constraints are met and appends value to accumulator.
    private void AppendAndVerify(ref KeyValueAccumulator accumulator, string decodedKey, string decodedValue)
    {
        accumulator.Append(decodedKey, decodedValue);

        if (accumulator.ValueCount > ValueCountLimit)
        {
            throw new InvalidDataException($"Form value count limit {ValueCountLimit} exceeded.");
        }
    }

    private string GetDecodedString(ReadOnlySpan<byte> readOnlySpan)
    {
        if (readOnlySpan.Length == 0)
        {
            return string.Empty;
        }
        else if (_encoding == Encoding.UTF8 || _encoding == Encoding.ASCII)
        {
            // UrlDecoder only works on UTF8 (and implicitly ASCII)

            // We need to create a Span from a ReadOnlySpan. This cast is safe because the memory is still held by the pipe
            // We will also create a string from it by the end of the function.
            var span = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(readOnlySpan), readOnlySpan.Length);

            try
            {
                var bytes = UrlDecoder.DecodeInPlace(span, isFormEncoding: true);
                span = span.Slice(0, bytes);

                return _encoding.GetString(span);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidDataException("The form value contains invalid characters.", ex);
            }
        }
        else
        {
            // Slow path for Unicode and other encodings.
            // Just do raw string replacement.
            var decodedString = _encoding.GetString(readOnlySpan);
            decodedString = decodedString.Replace('+', ' ');
            return Uri.UnescapeDataString(decodedString);
        }
    }

    private ReadOnlySpan<byte> GetEqualsForEncoding()
    {
        if (_encoding == Encoding.UTF8 || _encoding == Encoding.ASCII)
        {
            return UTF8EqualEncoded;
        }
        else
        {
            return _otherEqualEncoding;
        }
    }

    private ReadOnlySpan<byte> GetAndForEncoding()
    {
        if (_encoding == Encoding.UTF8 || _encoding == Encoding.ASCII)
        {
            return UTF8AndEncoded;
        }
        else
        {
            return _otherAndEncoding;
        }
    }
}
