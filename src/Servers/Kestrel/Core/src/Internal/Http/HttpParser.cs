// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

using BadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;

public class HttpParser<TRequestHandler> : IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler
{
    private readonly bool _showErrorDetails;

    public HttpParser() : this(showErrorDetails: true)
    {
    }

    public HttpParser(bool showErrorDetails)
    {
        _showErrorDetails = showErrorDetails;
    }

    // byte types don't have a data type annotation so we pre-cast them; to avoid in-place casts
    private const byte ByteCR = (byte)'\r';
    private const byte ByteLF = (byte)'\n';
    private const byte ByteColon = (byte)':';
    private const byte ByteSpace = (byte)' ';
    private const byte ByteTab = (byte)'\t';
    private const byte ByteQuestionMark = (byte)'?';
    private const byte BytePercentage = (byte)'%';
    private const int MinTlsRequestSize = 1; // We need at least 1 byte to check for a proper TLS request line

    public bool ParseRequestLine(TRequestHandler handler, ref SequenceReader<byte> reader)
    {
        if (reader.TryReadTo(out ReadOnlySpan<byte> requestLine, ByteLF, advancePastDelimiter: true))
        {
            ParseRequestLine(handler, requestLine);
            return true;
        }

        return false;
    }

    private void ParseRequestLine(TRequestHandler handler, ReadOnlySpan<byte> requestLine)
    {
        // Get Method and set the offset
        var method = requestLine.GetKnownMethod(out var methodEnd);
        if (method == HttpMethod.Custom)
        {
            methodEnd = GetUnknownMethodLength(requestLine);
        }

        var versionAndMethod = new HttpVersionAndMethod(method, methodEnd);

        // Use a new offset var as methodEnd needs to be on stack
        // as its passed by reference above so can't be in register.
        // Skip space
        var offset = methodEnd + 1;
        if ((uint)offset >= (uint)requestLine.Length)
        {
            // Start of path not found
            RejectRequestLine(requestLine);
        }

        var ch = requestLine[offset];
        if (ch == ByteSpace || ch == ByteQuestionMark || ch == BytePercentage)
        {
            // Empty path is illegal, or path starting with percentage
            RejectRequestLine(requestLine);
        }

        // Target = Path and Query
        var targetStart = offset;
        var pathEncoded = false;
        // Skip first char (just checked)
        offset++;

        // Find end of path and if path is encoded
        for (; (uint)offset < (uint)requestLine.Length; offset++)
        {
            ch = requestLine[offset];
            if (ch == ByteSpace || ch == ByteQuestionMark)
            {
                // End of path
                break;
            }
            else if (ch == BytePercentage)
            {
                pathEncoded = true;
            }
        }

        var path = new TargetOffsetPathLength(targetStart, length: offset - targetStart, pathEncoded);

        // Query string
        if (ch == ByteQuestionMark)
        {
            // We have a query string
            for (; (uint)offset < (uint)requestLine.Length; offset++)
            {
                ch = requestLine[offset];
                if (ch == ByteSpace)
                {
                    break;
                }
            }
        }

        var queryEnd = offset;
        // Consume space
        offset++;

        // Version + CR is 9 bytes which should take us to .Length
        // LF should have been dropped prior to method call
        if ((uint)offset + 9 != (uint)requestLine.Length || requestLine[offset + sizeof(ulong)] != ByteCR)
        {
            RejectRequestLine(requestLine);
        }

        // Version
        var remaining = requestLine.Slice(offset);
        var httpVersion = remaining.GetKnownVersion();
        versionAndMethod.Version = httpVersion;
        if (httpVersion == HttpVersion.Unknown)
        {
            // HTTP version is unsupported.
            RejectUnknownVersion(remaining);
        }

        // We need to reinterpret from ReadOnlySpan into Span to allow path mutation for
        // in-place normalization and decoding to transform into a canonical path
        var startLine = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(requestLine), queryEnd);
        handler.OnStartLine(versionAndMethod, path, startLine);
    }

    /// <summary>
    /// Finds '\r' or '\n' or '\t' or ' ' or '\t' in the given sequence (whatever comes first)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfTabOrSpaceOrColonOrCrOrLf(ref byte searchSpace, int length)
    {
        nuint ulen = (nuint)length;
        nuint offset = 0;

        // most inputs will be longer than 16 bytes so
        // let's prioritize SSE/NEON path
        // TODO: use AdvSimd for Arm64
        if (!Sse2.IsSupported || ulen < (nuint)Vector128<byte>.Count)
            goto SCALAR;

        var crVector = Vector128.Create((byte)'\r');
        var lfVector = Vector128.Create((byte)'\n');
        var tbVector = Vector128.Create((byte)'\t');
        var wsVector = Vector128.Create((byte)' ');
        var clVector = Vector128.Create((byte)':');

    NEXT_VECTOR:
        var search = Unsafe.ReadUnaligned<Vector128<byte>>(
            ref Unsafe.AddByteOffset(ref searchSpace, offset));

        // pipeline SIMD operations
        var cmp0 = Sse2.CompareEqual(crVector, search);
        var cmp1 = Sse2.CompareEqual(lfVector, search);
        var cmp2 = Sse2.CompareEqual(tbVector, search);
        var cmp3 = Sse2.CompareEqual(wsVector, search);
        var cmp4 = Sse2.CompareEqual(clVector, search);

        // For some reason JIT may still re-order some :'(
        var or01 = Sse2.Or(cmp0, cmp1);
        var or23 = Sse2.Or(cmp2, cmp3);
        var orAll = Sse2.Or(Sse2.Or(or01, cmp4), or23);

        int matches = Sse2.MoveMask(orAll);
        if (matches != 0)
            return (int)(offset + (nuint)BitOperations.TrailingZeroCount(matches));

        offset += (nuint)Vector128<byte>.Count;
        if (offset == ulen)
            // we're done and nothing was found
            return -1;
        if (offset + (nuint)Vector128<byte>.Count > ulen)
            // not enough space for the next 128bit vector so let's overlap
            // with the current one in order to avoid SCALAR fallback
            offset = ulen - (nuint)Vector128<byte>.Count;
        goto NEXT_VECTOR;

    SCALAR:
        for (; offset < ulen; offset++)
        {
            byte val = Unsafe.AddByteOffset(ref searchSpace, offset);

            // Here we want to do something like this:
            //
            // if (val == '\r' || val == '\n' || val == '\t' || val == ' ' || val == ':')
            //     return (int)offset;
            //
            // (or a switch operator)
            //
            // but unfortunately neither JIT nor Roslyn are smart enough to lower it to bit-test
            // PS: JIT still emits sub-optimal codegen here and doesn't recognize 'bt'
            //
            val = (byte)(val - 9); // lower limit is '\t'
            if (val > 49) // upper limit is ':'
                continue;

            const ulong bitMask = 0b10000000000000000000000000100000000000000000010011;
            //                      :                        ' '                 r  nt
            if (((bitMask >> val) & 1) == 1)
                return (int)offset;
        }
        return -1;
    }

    /// <summary>
    /// Find CR or LF in the given sequence (whatever comes first)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfCrOrLf(ref byte searchSpace, int length)
    {
        nuint ulen = (nuint)length;
        nuint offset = 0;

        // most inputs will be longer than 16 bytes so
        // let's prioritize SSE/NEON path
        // TODO: use AdvSimd for Arm64
        if (!Sse2.IsSupported || ulen < (nuint)Vector128<byte>.Count)
            goto SCALAR;

        var crVector = Vector128.Create((byte)'\r');
        var lfVector = Vector128.Create((byte)'\n');

    NEXT_VECTOR:
        var search = Unsafe.ReadUnaligned<Vector128<byte>>(
            ref Unsafe.AddByteOffset(ref searchSpace, offset));

        var cmp0 = Sse2.CompareEqual(crVector, search);
        var cmp1 = Sse2.CompareEqual(lfVector, search);

        int matches = Sse2.MoveMask(Sse2.Or(cmp0, cmp1));
        if (matches != 0)
            return (int)(offset + (nuint)BitOperations.TrailingZeroCount(matches));

        offset += (nuint)Vector128<byte>.Count;
        if (offset == ulen)
            // we're done
            return -1;
        if (offset + (nuint)Vector128<byte>.Count > ulen)
            // not enough space for the next 128bit vector so let's overlap
            // with the current one in order to avoid SCALAR fallback
            offset = ulen - (nuint)Vector128<byte>.Count;
        goto NEXT_VECTOR;

    SCALAR:
        for (; offset < ulen; offset++)
        {
            // if (val == '\r' || val == '\n' || val == '\t' || val == ' ' || val == ':')
            // the following code is a "bittest" version of ^:
            var val = Unsafe.AddByteOffset(ref searchSpace, offset);
            if (val == '\r' || val == '\n')
                return (int)offset;
        }
        return -1;
    }

    /// <summary>
    /// Finds "name: value\r\n" in the given span.
    /// Rules:
    ///  1) name should not be empty
    ///  2) name is ended with ':'
    ///  3) name should not contain ' ' or '\t'
    ///  4) value can be surrounded by multiple ' ' or\and '\t' and we must trim them out for OnHeader
    ///  5) name-value must be followed by \r\n (CRLF)
    ///  6) CR or\and LF are not expected anywhere else
    /// </summary>
    /// <returns>
    ///  -1: in case of invalid sequence
    ///   0: in case if it needs more data from span
    /// >=0: length of the current header line
    /// </returns>
    private static int TryTakeSingleHeaderFast(TRequestHandler handler, ReadOnlySpan<byte> span)
    {
        ref byte searchSpace = ref MemoryMarshal.GetReference(span);
        int colonPos = IndexOfTabOrSpaceOrColonOrCrOrLf(ref searchSpace, span.Length);

        // colon was not found - needs more data
        if (colonPos == -1)
            goto NOT_ENOUGH_DATA;

        // name should not be empty
        if (colonPos == 0)
            goto FAILED;

        ref byte colonValue = ref Unsafe.AddByteOffset(ref searchSpace, (nuint)colonPos);

        // none of '\r', '\n', '\t', ' ' should be found before ':'
        if (colonValue != ByteColon)
            goto FAILED;

        int crlf = IndexOfCrOrLf(ref colonValue, span.Length - colonPos);

        // CR was not found - needs more data
        if (crlf == -1)
            goto NOT_ENOUGH_DATA;

        int crPos = crlf + colonPos;

        ref byte crRef = ref Unsafe.AddByteOffset(ref colonValue, (nuint)crlf);

        // for some reason previous version of parser gave up on inputs
        // like "X:\r\n", however, "X: \r\n" was fine for it
        // should we care about it here?
        if (/*crPos < 3 ||*/ crRef != ByteCR)
            goto FAILED;

        // no room for LF - needs more data
        if (crPos == span.Length - 1)
            goto NOT_ENOUGH_DATA;

        // check next symbol after CR, it has to be LF
        if (Unsafe.AddByteOffset(ref crRef, 1) != ByteLF)
            goto FAILED;

        // Trim leading ' ' and '\t'
        int valueStarts = colonPos + 1;
        for (; valueStarts < crPos; valueStarts++)
        {
            var val = Unsafe.AddByteOffset(ref searchSpace, (nuint)valueStarts);
            if (val != ByteSpace && val != ByteTab)
                break;
        }

        // Trim trailing ' ' and '\t'
        int valueEnds = crPos - 1;
        for (; valueEnds > valueStarts; valueEnds--)
        {
            var b = Unsafe.AddByteOffset(ref searchSpace, (nuint)valueEnds);
            if (b != ByteSpace && b != ByteTab)
                break;
        }

        handler.OnHeader(span.Slice(0, colonPos), span.Slice(valueStarts, (valueEnds + 1 - valueStarts)));
        return crPos + 2;

    FAILED:
        return -1;

    NOT_ENOUGH_DATA:
        return 0;
    }

    public bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader)
    {
        while (!reader.End)
        {
            var span = reader.UnreadSpan;
            while (span.Length > 0)
            {
                byte ch1;
                var ch2 = (byte)0;
                var readAhead = 0;

                // Fast path, we're still looking at the same span
                if (span.Length >= 2)
                {
                    ch1 = span[0];
                    ch2 = span[1];
                }
                else if (reader.TryRead(out ch1)) // Possibly split across spans
                {
                    // Note if we read ahead by 1 or 2 bytes
                    readAhead = (reader.TryRead(out ch2)) ? 2 : 1;
                }

                if (ch1 == ByteCR)
                {
                    // Check for final CRLF.
                    if (ch2 == ByteLF)
                    {
                        // If we got 2 bytes from the span directly so skip ahead 2 so that
                        // the reader's state matches what we expect
                        if (readAhead == 0)
                        {
                            reader.Advance(2);
                        }

                        // Double CRLF found, so end of headers.
                        handler.OnHeadersComplete(endStream: false);
                        return true;
                    }
                    else if (readAhead == 1)
                    {
                        // Didn't read 2 bytes, reset the reader so we don't consume anything
                        reader.Rewind(1);
                        return false;
                    }

                    Debug.Assert(readAhead == 0 || readAhead == 2);
                    // Headers don't end in CRLF line.

                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidRequestHeadersNoCRLF);
                }

                var length = 0;
                // We only need to look for the end if we didn't read ahead; otherwise there isn't enough in
                // in the span to contain a header.
                if (readAhead == 0)
                {
                    length = TryTakeSingleHeaderFast(handler, span);
                    if (length > 0)
                    {
                        span = span.Slice(length);
                        reader.Advance(length);
                        continue;
                    }
                    if (length == -1)
                    {
                        // do we need to limit span here as it was before?
                        RejectRequestHeader(span);
                    }
                }

                // End found in current span
                if (length > 0)
                {
                    continue;
                }

                // We moved the reader to look ahead 2 bytes so rewind the reader
                if (readAhead > 0)
                {
                    reader.Rewind(readAhead);
                }

                length = ParseMultiSpanHeader(handler, ref reader);
                if (length < 0)
                {
                    // Not there
                    return false;
                }

                reader.Advance(length);
                // As we crossed spans set the current span to default
                // so we move to the next span on the next iteration
                span = default;
            }
        }

        return false;
    }

    private int ParseMultiSpanHeader(TRequestHandler handler, ref SequenceReader<byte> reader)
    {
        var currentSlice = reader.UnreadSequence;
        var lineEndPosition = currentSlice.PositionOfAny(ByteCR, ByteLF);

        if (lineEndPosition == null)
        {
            // Not there.
            return -1;
        }

        SequencePosition lineEnd;
        ReadOnlySpan<byte> headerSpan;
        if (currentSlice.Slice(reader.Position, lineEndPosition.Value).Length == currentSlice.Length - 1)
        {
            // No enough data, so CRLF can't currently be there.
            // However, we need to check the found char is CR and not LF

            // Advance 1 to include CR/LF in lineEnd
            lineEnd = currentSlice.GetPosition(1, lineEndPosition.Value);
            headerSpan = currentSlice.Slice(reader.Position, lineEnd).ToSpan();
            if (headerSpan[^1] != ByteCR)
            {
                RejectRequestHeader(headerSpan);
            }
            return -1;
        }

        // Advance 2 to include CR{LF?} in lineEnd
        lineEnd = currentSlice.GetPosition(2, lineEndPosition.Value);
        headerSpan = currentSlice.Slice(reader.Position, lineEnd).ToSpan();

        if (headerSpan.Length < 5)
        {
            // Less than min possible headerSpan is 5 bytes a:b\r\n
            RejectRequestHeader(headerSpan);
        }

        if (headerSpan[^2] != ByteCR)
        {
            // Sequence needs to be CRLF not LF first.
            RejectRequestHeader(headerSpan[..^1]);
        }

        if (headerSpan[^1] != ByteLF ||
            // Exclude the CRLF from the headerLine and parse the header name:value pair
            !TryTakeSingleHeader(handler, headerSpan[..^2]))
        {
            // Sequence needs to be CRLF and not contain an inner CR not part of terminator.
            // Not parsable as a valid name:value header pair.
            RejectRequestHeader(headerSpan);
        }

        return headerSpan.Length;
    }

    private static bool TryTakeSingleHeader(TRequestHandler handler, ReadOnlySpan<byte> headerLine)
    {
        // We are looking for a colon to terminate the header name.
        // However, the header name cannot contain a space or tab so look for all three
        // and see which is found first.
        var nameEnd = headerLine.IndexOfAny(ByteColon, ByteSpace, ByteTab);
        // If not found length with be -1; casting to uint will turn it to uint.MaxValue
        // which will be larger than any possible headerLine.Length. This also serves to eliminate
        // the bounds check for the next lookup of headerLine[nameEnd]
        if ((uint)nameEnd >= (uint)headerLine.Length)
        {
            // Colon not found.
            return false;
        }

        // Early memory read to hide latency
        var expectedColon = headerLine[nameEnd];
        if (nameEnd == 0)
        {
            // Header name is empty.
            return false;
        }
        if (expectedColon != ByteColon)
        {
            // Header name space or tab.
            return false;
        }

        // Skip colon to get to the value start.
        var valueStart = nameEnd + 1;

        // Generally there will only be one space, so we will check it directly
        if ((uint)valueStart < (uint)headerLine.Length)
        {
            var ch = headerLine[valueStart];
            if (ch == ByteSpace || ch == ByteTab)
            {
                // Ignore first whitespace.
                valueStart++;

                // More header chars?
                if ((uint)valueStart < (uint)headerLine.Length)
                {
                    ch = headerLine[valueStart];
                    // Do a fast check; as we now expect non-space, before moving into loop.
                    if (ch <= ByteSpace && (ch == ByteSpace || ch == ByteTab))
                    {
                        valueStart++;
                        // Is more whitespace, so we will loop to find the end. This is the slow path.
                        for (; valueStart < headerLine.Length; valueStart++)
                        {
                            ch = headerLine[valueStart];
                            if (ch != ByteTab && ch != ByteSpace)
                            {
                                // Non-whitespace char found, valueStart is now start of value.
                                break;
                            }
                        }
                    }
                }
            }
        }

        var valueEnd = headerLine.Length - 1;
        // Ignore end whitespace. Generally there will no spaces
        // so we will check the first before moving to a loop.
        if (valueEnd > valueStart)
        {
            var ch = headerLine[valueEnd];
            // Do a fast check; as we now expect non-space, before moving into loop.
            if (ch <= ByteSpace && (ch == ByteSpace || ch == ByteTab))
            {
                // Is whitespace so move to loop
                valueEnd--;
                for (; valueEnd > valueStart; valueEnd--)
                {
                    ch = headerLine[valueEnd];
                    if (ch != ByteTab && ch != ByteSpace)
                    {
                        // Non-whitespace char found, valueEnd is now start of value.
                        break;
                    }
                }
            }
        }

        // Range end is exclusive, so add 1 to valueEnd
        valueEnd++;
        handler.OnHeader(name: headerLine[..nameEnd], value: headerLine[valueStart..valueEnd]);

        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int GetUnknownMethodLength(ReadOnlySpan<byte> span)
    {
        var invalidIndex = HttpCharacters.IndexOfInvalidTokenChar(span);

        if (invalidIndex <= 0 || span[invalidIndex] != ByteSpace)
        {
            RejectRequestLine(span);
        }

        return invalidIndex;
    }

    private static bool IsTlsHandshake(ReadOnlySpan<byte> requestLine)
    {
        const byte SslRecordTypeHandshake = (byte)0x16;

        // Make sure we can check at least for the existence of a TLS handshake - we check the first byte
        // See https://serializethoughts.com/2014/07/27/dissecting-tls-client-hello-message/

        return (requestLine.Length >= MinTlsRequestSize && requestLine[0] == SslRecordTypeHandshake);
    }

    [StackTraceHidden]
    private void RejectRequestLine(ReadOnlySpan<byte> requestLine)
    {
        throw GetInvalidRequestException(
            IsTlsHandshake(requestLine) ?
            RequestRejectionReason.TlsOverHttpError :
            RequestRejectionReason.InvalidRequestLine,
            requestLine);
    }

    [StackTraceHidden]
    private void RejectRequestHeader(ReadOnlySpan<byte> headerLine)
        => throw GetInvalidRequestException(RequestRejectionReason.InvalidRequestHeader, headerLine);

    [StackTraceHidden]
    private void RejectUnknownVersion(ReadOnlySpan<byte> version)
        => throw GetInvalidRequestException(RequestRejectionReason.UnrecognizedHTTPVersion, version[..^1]);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private BadHttpRequestException GetInvalidRequestException(RequestRejectionReason reason, ReadOnlySpan<byte> headerLine)
        => KestrelBadHttpRequestException.GetException(
            reason,
            _showErrorDetails
                ? headerLine.GetAsciiStringEscaped(Constants.MaxExceptionDetailSize)
                : string.Empty);
}
