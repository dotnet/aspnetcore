// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

using BadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;

/// <summary>
/// This API supports framework infrastructure and is not intended to be used
/// directly from application code.
/// </summary>
/// <typeparam name="TRequestHandler">This API supports framework infrastructure and is not intended to be used
/// directly from application code.</typeparam>
public class HttpParser<TRequestHandler> : IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler
{
    private readonly bool _showErrorDetails;
    private readonly bool _disableHttp1LineFeedTerminators;

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public HttpParser() : this(showErrorDetails: true)
    {
    }

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public HttpParser(bool showErrorDetails) : this(showErrorDetails, AppContext.TryGetSwitch(KestrelServerOptions.DisableHttp1LineFeedTerminatorsSwitchKey, out var disabled) && disabled)
    {
    }

    internal HttpParser(bool showErrorDetails, bool disableHttp1LineFeedTerminators)
    {
        _showErrorDetails = showErrorDetails;
        _disableHttp1LineFeedTerminators = disableHttp1LineFeedTerminators;
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
    private static ReadOnlySpan<byte> RequestLineDelimiters => [ByteLF, 0];

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public bool ParseRequestLine(TRequestHandler handler, ref SequenceReader<byte> reader)
    {
        // Find the next delimiter.
        if (!reader.TryReadToAny(out ReadOnlySpan<byte> requestLine, RequestLineDelimiters, advancePastDelimiter: false))
        {
            return false;
        }

        // Consume the delimiter.
        var foundDelimiter = reader.TryRead(out var next);
        Debug.Assert(foundDelimiter);
        // If null character found, or request line is empty
        if (next == 0 || requestLine.Length == 0)
        {
            // Rewind and re-read to format error message correctly
            reader.Rewind(requestLine.Length + 1);
            var readResult = reader.TryReadExact(requestLine.Length + 1, out var requestLineSequence);
            Debug.Assert(readResult);
            requestLine = requestLineSequence.IsSingleSegment ? requestLineSequence.FirstSpan : requestLineSequence.ToArray();
            RejectRequestLine(requestLine);
        }

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
        var index = requestLine.Slice(offset).IndexOfAny(ByteSpace, ByteQuestionMark, BytePercentage);
        if (index >= 0)
        {
            if (requestLine[offset + index] == BytePercentage)
            {
                pathEncoded = true;
                offset += index;
                // Found an encoded character, now just search for end of path
                index = requestLine.Slice(offset).IndexOfAny(ByteSpace, ByteQuestionMark);
            }

            offset += index;
            ch = requestLine[offset];
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

        while ((uint)offset < (uint)requestLine.Length
            && requestLine[offset] == ByteSpace)
        {
            // It's invalid to have multiple spaces between the url resource and version
            // but some clients do it. Skip them.
            offset++;
        }

        // Version + CR is 9 bytes which should take us to .Length
        // LF should have been dropped prior to method call
        if ((uint)offset + 9 != (uint)requestLine.Length || requestLine[offset + 8] != ByteCR)
        {
            // LF should have been dropped prior to method call
            // If !_disableHttp1LineFeedTerminators and offset + 8 is .Length,
            // then requestLine is valid since it means LF was the next char
            if (_disableHttp1LineFeedTerminators || (uint)offset + 8 != (uint)requestLine.Length)
            {
                RejectRequestLine(requestLine);
            }
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

        return true;
    }

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader)
    {
        while (!reader.End)
        {
            // Check if the reader's span contains an LF to skip the reader if possible
            var span = reader.UnreadSpan;

            // Fast path, CR/LF at the beginning
            if (span.Length >= 2 && span[0] == ByteCR && span[1] == ByteLF)
            {
                reader.Advance(2);
                handler.OnHeadersComplete(endStream: false);
                return true;
            }

            var foundCrlf = false;

            var lfOrCrIndex = span.IndexOfAny(ByteCR, ByteLF);
            if (lfOrCrIndex >= 0)
            {
                if (span[lfOrCrIndex] == ByteCR)
                {
                    // We got a CR. Is this a CR/LF sequence?
                    var crIndex = lfOrCrIndex;
                    reader.Advance(crIndex + 1);

                    bool hasDataAfterCr;

                    if ((uint)span.Length > (uint)(crIndex + 1) && span[crIndex + 1] == ByteLF)
                    {
                        // CR/LF in the same span (common case)
                        span = span.Slice(0, crIndex);
                        foundCrlf = true;
                    }
                    else if ((hasDataAfterCr = reader.TryPeek(out byte lfMaybe)) && lfMaybe == ByteLF)
                    {
                        // CR/LF but split between spans
                        span = span.Slice(0, span.Length - 1);
                        foundCrlf = true;
                    }
                    else
                    {
                        // What's after the CR?
                        if (!hasDataAfterCr)
                        {
                            // No more chars after CR? Don't consume an incomplete header
                            reader.Rewind(crIndex + 1);
                            return false;
                        }
                        else if (crIndex == 0)
                        {
                            // CR followed by something other than LF
                            KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidRequestHeadersNoCRLF);
                        }
                        else
                        {
                            // Include the thing after the CR in the rejection exception.
                            var stopIndex = crIndex + 2;
                            RejectRequestHeader(span[..stopIndex]);
                        }
                    }

                    if (foundCrlf)
                    {
                        // Advance past the LF too
                        reader.Advance(1);

                        // Empty line?
                        if (crIndex == 0)
                        {
                            handler.OnHeadersComplete(endStream: false);
                            return true;
                        }
                    }
                }
                else
                {
                    // We got an LF with no CR before it.
                    var lfIndex = lfOrCrIndex;
                    if (_disableHttp1LineFeedTerminators)
                    {
                        RejectRequestHeader(AppendEndOfLine(span[..lfIndex], lineFeedOnly: true));
                    }

                    // Consume the header including the LF
                    reader.Advance(lfIndex + 1);

                    span = span.Slice(0, lfIndex);
                    if (span.Length == 0)
                    {
                        handler.OnHeadersComplete(endStream: false);
                        return true;
                    }
                }
            }
            else
            {
                // No CR or LF. Is this a multi-span header?
                int length = ParseMultiSpanHeader(handler, ref reader);
                if (length < 0)
                {
                    // Not multi-line, just bad.
                    return false;
                }

                // This was a multi-line header. Advance the reader.
                reader.Advance(length);

                continue;
            }

            // We got to a point where we believe we have a header.
            if (!TryTakeSingleHeader(handler, span))
            {
                // Sequence needs to be CRLF and not contain an inner CR not part of terminator.
                // Not parsable as a valid name:value header pair.
                RejectRequestHeader(AppendEndOfLine(span, lineFeedOnly: !foundCrlf));
            }
        }

        return false;
    }

    private static byte[] AppendEndOfLine(ReadOnlySpan<byte> span, bool lineFeedOnly)
    {
        var array = new byte[span.Length + (lineFeedOnly ? 1 : 2)];

        span.CopyTo(array);
        array[^1] = ByteLF;

        if (!lineFeedOnly)
        {
            array[^2] = ByteCR;
        }

        return array;
    }

    // Parse a header that might cross multiple spans, and return the length of the header
    // or -1 if there was a failure during parsing.
    private int ParseMultiSpanHeader(TRequestHandler handler, ref SequenceReader<byte> reader)
    {
        var currentSlice = reader.UnreadSequence;

        SequencePosition position = currentSlice.Start;

        // Skip the first segment as the caller already searched it for CR/LF
        var result = currentSlice.TryGet(ref position, out ReadOnlyMemory<byte> memory);
        // there will always be at least 1 segment so this will never return false
        Debug.Assert(result);

        if (position.GetObject() == null)
        {
            // Only 1 segment in the reader currently, this is a partial header, wait for more data
            return -1;
        }

        var index = -1;
        var headerLength = memory.Length;
        while (currentSlice.TryGet(ref position, out memory))
        {
            index = memory.Span.IndexOfAny(ByteCR, ByteLF);
            if (index >= 0)
            {
                headerLength += index;
                break;
            }
            else if (position.GetObject() == null)
            {
                return -1;
            }

            headerLength += memory.Length;
        }

        // No CR or LF found in the SequenceReader
        if (index == -1)
        {
            return -1;
        }

        // Is the first EOL char the last of the current slice?
        if (headerLength == currentSlice.Length - 1)
        {
            // Check the EOL char
            if (memory.Span[index] == ByteCR)
            {
                // CR without LF, can't read the header
                return -1;
            }
            else
            {
                if (_disableHttp1LineFeedTerminators)
                {
                    // LF only but disabled

                    // Advance 1 to include LF in result
                    RejectRequestHeader(currentSlice.Slice(0, headerLength + 1).ToSpan());
                }
            }
        }

        ReadOnlySequence<byte> header;
        if (memory.Span[index] == ByteCR)
        {
            // First EOL char is CR, include the char after CR
            // Advance 2 to include CR and LF
            headerLength += 2;
            header = currentSlice.Slice(0, headerLength);
        }
        else if (_disableHttp1LineFeedTerminators)
        {
            // The terminator is an LF and we don't allow it.
            // Advance 1 to include LF in result
            RejectRequestHeader(currentSlice.Slice(0, headerLength + 1).ToSpan());
            return -1;
        }
        else
        {
            // First EOL char is LF. only include this one
            headerLength += 1;
            header = currentSlice.Slice(0, headerLength);
        }

        // 'a:b\n' or 'a:b\r\n'
        var minHeaderSpan = _disableHttp1LineFeedTerminators ? 5 : 4;
        if (headerLength < minHeaderSpan)
        {
            RejectRequestHeader(currentSlice.Slice(0, headerLength).ToSpan());
        }

        byte[]? array = null;
        Span<byte> headerSpan = headerLength <= 256 ? stackalloc byte[256] : array = ArrayPool<byte>.Shared.Rent(headerLength);

        header.CopyTo(headerSpan);
        headerSpan = headerSpan.Slice(0, headerLength);

        var terminatorSize = -1;

        if (headerSpan[^1] == ByteLF)
        {
            if (headerSpan[^2] == ByteCR)
            {
                terminatorSize = 2;
            }
            else if (!_disableHttp1LineFeedTerminators)
            {
                terminatorSize = 1;
            }
        }

        // Last chance to bail if the terminator size is not valid or the header doesn't parse.
        if (terminatorSize == -1 || !TryTakeSingleHeader(handler, headerSpan.Slice(0, headerSpan.Length - terminatorSize)))
        {
            RejectRequestHeader(headerSpan);
        }

        if (array is not null)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        return headerLength;
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
        handler.OnHeader(name: headerLine.Slice(0, nameEnd), value: headerLine[valueStart..valueEnd]);

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
