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
    private readonly bool _allowLineFeedTerminator;

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public HttpParser() : this(showErrorDetails: true, allowLineFeedTerminator: false)
    {
    }

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public HttpParser(bool showErrorDetails) : this(showErrorDetails, allowLineFeedTerminator: false)
    {
        _showErrorDetails = showErrorDetails;
    }

    internal HttpParser(bool showErrorDetails, bool allowLineFeedTerminator)
    {
        _showErrorDetails = showErrorDetails;
        _allowLineFeedTerminator = allowLineFeedTerminator;
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

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
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
        if ((uint)offset + 9 != (uint)requestLine.Length || requestLine[offset + 8] != ByteCR)
        {
            // LF should have been dropped prior to method call
            // If _allowLineFeedTerminator and offset + 8 is .Length,
            // then requestLine is valid since it means LF was the next char
            if (!_allowLineFeedTerminator || (uint)offset + 8 != (uint)requestLine.Length)
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
    }

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    public bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader)
    {
        while (!reader.End)
        {
            // Size of the detected EOL
            var eolSize = -1;

            // Check if the reader's span contains an LF to skip the reader if possible
            var span = reader.UnreadSpan;

            // Fast path, CR/LF at the beginning
            if (span.Length >= 2 && span[0] == ByteCR && span[1] == ByteLF)
            {
                reader.Advance(2);
                handler.OnHeadersComplete(endStream: false);
                return true;
            }

            var lfOrCrIndex = span.IndexOfAny(ByteCR, ByteLF);
            if (lfOrCrIndex >= 0)
            {
                if (span[lfOrCrIndex] == ByteCR)
                {
                    // We got a CR. Is this a CR/LF sequence?
                    var crIndex = lfOrCrIndex;
                    reader.Advance(crIndex + 1);

                    var foundCrlf = false;
                    if ((uint)span.Length > (uint)(crIndex + 1) && span[crIndex + 1] == ByteLF)
                    {
                        // CR/LF in the same span (common case)
                        span = span.Slice(0, crIndex);
                        foundCrlf = true;
                    }
                    else if (reader.TryPeek(out byte lfMaybe) && lfMaybe == ByteLF)
                    {
                        // CR/LF but split between spans
                        span = span.Slice(0, span.Length - 1);
                        foundCrlf = true;
                    }
                    else
                    {
                        // What's after the CR?
                        if (!reader.TryPeek(out _))
                        {
                            // No more chars after CR? Don't consume an incomplete header
                            reader.Rewind(crIndex + 1);
                            return false;
                        }
                        else if (crIndex == 0)
                        {
                            // CR by itself
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
                        eolSize = 2;

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
                    if (!_allowLineFeedTerminator)
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
                RejectRequestHeader(AppendEndOfLine(span, lineFeedOnly: eolSize == 1));
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
        var lineEndPosition = currentSlice.PositionOfAny(ByteCR, ByteLF);

        if (lineEndPosition == null)
        {
            // Not there.
            return -1;
        }

        SequencePosition lineEnd;
        scoped ReadOnlySpan<byte> headerSpan;
        ReadOnlySequence<byte> header;
        if (!_allowLineFeedTerminator && (currentSlice.Slice(reader.Position, lineEndPosition.Value).Length == currentSlice.Length - 1))
        {
            // If we're not allowing LF by itself as a terminator, we need two characters
            // for the line terminator. Since we don't have two, we know there can't be
            // a CRLF here. However, we need to also check that found char is CR and not LF.

            // Advance 1 to include CR/LF in lineEnd
            lineEnd = currentSlice.GetPosition(1, lineEndPosition.Value);
            header = currentSlice.Slice(reader.Position, lineEnd);
            headerSpan = header.IsSingleSegment ? header.FirstSpan : header.ToArray();
            if (headerSpan[^1] != ByteCR)
            {
                RejectRequestHeader(headerSpan);
            }

            return -1;
        }

        // Offset 1 to include the first line end char.
        var firstLineEndCharPos = currentSlice.GetPosition(1, lineEndPosition.Value);
        header = currentSlice.Slice(reader.Position, firstLineEndCharPos);

        if (header.ToSpan()[^1] == ByteCR)
        {
            // Advance one more to include the potential LF in lineEnd
            lineEnd = currentSlice.GetPosition(1, firstLineEndCharPos);
            header = currentSlice.Slice(reader.Position, lineEnd);
        }
        else if (!_allowLineFeedTerminator)
        {
            // The terminator is an LF and we don't allow it.
            headerSpan = header.IsSingleSegment ? header.FirstSpan : header.ToArray();
            RejectRequestHeader(headerSpan);
            return -1;
        }

        headerSpan = header.IsSingleSegment ? header.FirstSpan : header.ToArray();

        // 'a:b\n' or 'a:b\r\n'
        var minHeaderSpan = _allowLineFeedTerminator ? 4 : 5;
        if (headerSpan.Length < minHeaderSpan)
        {
            RejectRequestHeader(headerSpan);
        }

        var terminatorSize = -1;
        if (headerSpan[^1] == ByteLF)
        {
            if (headerSpan[^2] == ByteCR)
            {
                terminatorSize = 2;
            }
            else if (_allowLineFeedTerminator)
            {
                terminatorSize = 1;
            }
        }

        // Last chance to bail if the terminator size is not valid or the header doesn't parse.
        if (terminatorSize == -1 || !TryTakeSingleHeader(handler, headerSpan[..^terminatorSize]))
        {
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
