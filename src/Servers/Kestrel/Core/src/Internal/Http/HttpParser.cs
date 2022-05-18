// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    using BadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;

    public class HttpParser<TRequestHandler> : IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler
    {
        private readonly bool _showErrorDetails;
        private readonly bool _enableLineFeedTerminator;

        public HttpParser() : this(showErrorDetails: true, enableLineFeedTerminator: false)
        {
        }

        public HttpParser(bool showErrorDetails) : this(showErrorDetails, enableLineFeedTerminator: false)
        {
        }

        internal HttpParser(bool showErrorDetails, bool enableLineFeedTerminator)
        {
            _showErrorDetails = showErrorDetails;
            _enableLineFeedTerminator = enableLineFeedTerminator;
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
            // Skip any leading \r or \n on the request line. This is not technically allowed,
            // but apparently there are enough clients relying on this that it's worth allowing.
            // Peek first as a minor performance optimization; it's a quick inlined check.
            if (reader.TryPeek(out byte b) && (b == ByteCR || b == ByteLF))
            {
                reader.AdvancePastAny(ByteCR, ByteLF);
            }

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
                // If _enableLineFeedTerminator and offset + 8 is .Length,
                // then requestLine is valid since it mean LF was the next char
                if (!_enableLineFeedTerminator || (uint)offset + 8 != (uint)requestLine.Length)
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

        public bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader)
        {
            while (!reader.End)
            {
                var span = reader.UnreadSpan;

                // Size of header in the current span, if known
                var length = -1;

                while (span.Length > 0)
                {
                    // The size of the EOL terminator. Always -1 (no valid EOL), 1 (LF) or 2 (CRLF)
                    var eolSize = -1;

                    // length can be set when the span is returned by ParseMultiSpanHeader
                    if (length == -1)
                    {
                        length = span.IndexOfAny(ByteCR, ByteLF);
                    }

                    if (length != -1)
                    {
                        // Validate the EOL terminator
                        eolSize = ParseHeaderLineEnd(span, length);

                        // Not valid
                        if (eolSize == -1)
                        {
                            length = -1;
                        }
                    }

                    // Empty header (EOL only)?
                    if (length == 0)
                    {
                        handler.OnHeadersComplete(endStream: false);
                        reader.Advance(eolSize);
                        return true;
                    }

                    // If not found length will be -1; casting to uint will turn it to uint.MaxValue
                    // which will be larger than any possible span.Length. This also serves to eliminate
                    // the bounds check for the next lookup of span[length]
                    if ((uint)length < (uint)span.Length)
                    {
                        var lineLength = length + eolSize;

                        if (length != 0 && !TryTakeSingleHeader(handler, span[..length]))
                        {
                            // Sequence needs to be CRLF and not contain an inner CR not part of terminator.
                            // Not parsable as a valid name:value header pair.
                            RejectRequestHeader(span[..lineLength]);
                        }

                        // Read the header successfully, skip the reader forward past the headerSpan.
                        span = span[lineLength..];
                        reader.Advance(lineLength);
                    }

                    // End found in current span
                    if (length > 0)
                    {
                        length = -1;
                        continue;
                    }

                    // Load next header line to parse as a span
                    span = ParseMultiSpanHeader(ref reader, out length);

                    // If there any remaining line?
                    if (length == -1 && span.Length == 0)
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        // Returns the length of the line terminator (CRLF = 2, LF = 1)
        // If no valid EOL is detected then -1
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int ParseHeaderLineEnd(ReadOnlySpan<byte> headerSpan, int headerLineLength)
        {
            // This method needs to be called with a positive value representing the index of either CR or LF
            Debug.Assert(headerLineLength >= 0);

            if (headerSpan[headerLineLength] == ByteCR)
            {
                // No more chars after CR? Don't consume an incomplete header
                if (headerSpan.Length == headerLineLength + 1)
                {
                    return -1;
                }

                // CR must be followed by LF in all cases
                if (headerSpan[headerLineLength + 1] != ByteLF)
                {
                    if (headerLineLength == 0)
                    {
                        KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidRequestHeadersNoCRLF);
                    }
                    else
                    {
                        RejectRequestHeader(headerSpan[..(headerLineLength + 2)]);
                    }
                }

                return 2;
            }

            if (_enableLineFeedTerminator)
            {
                return 1;
            }

            // LF but not allowed
            RejectRequestHeader(headerSpan[..(headerLineLength + 1)]);

            return 0;
        }

        // Returns a span from the remaining sequence until the next valid EOL
        private ReadOnlySpan<byte> ParseMultiSpanHeader(ref SequenceReader<byte> reader, out int length)
        {
            length = -1;

            var currentSlice = reader.UnreadSequence;
            var lineEndPosition = currentSlice.PositionOfAny(ByteCR, ByteLF);

            if (lineEndPosition == null)
            {
                return ReadOnlySpan<byte>.Empty;
            }

            SequencePosition lineEnd;
            ReadOnlySpan<byte> headerSpan;
            if (currentSlice.Slice(reader.Position, lineEndPosition.Value).Length == currentSlice.Length - 1)
            {
                // No enough data, so CRLF can't currently be there.
                // However, we need to check the found char is CR and not LF (unless quirk mode)

                // Advance 1 to include CR/LF in lineEnd
                lineEnd = currentSlice.GetPosition(1, lineEndPosition.Value);
                headerSpan = currentSlice.Slice(reader.Position, lineEnd).ToSpan();

                if (headerSpan[^1] == ByteLF)
                {
                    length = headerSpan.Length - 1;
                    return headerSpan;
                }

                return ReadOnlySpan<byte>.Empty;
            }

            // Advance 2 to include CR{LF?} in lineEnd
            lineEnd = currentSlice.GetPosition(2, lineEndPosition.Value);
            headerSpan = currentSlice.Slice(reader.Position, lineEnd).ToSpan();

            length = headerSpan.Length - 2;
            return headerSpan;
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
}
