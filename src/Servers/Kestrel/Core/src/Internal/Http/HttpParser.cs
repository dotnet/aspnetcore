// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
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

        public unsafe bool ParseRequestLine(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;
            examined = buffer.End;

            // Prepare the first span
            var span = buffer.FirstSpan;
            var lineIndex = span.IndexOf(ByteLF);
            if (lineIndex >= 0)
            {
                consumed = buffer.GetPosition(lineIndex + 1, consumed);
                span = span.Slice(0, lineIndex + 1);
            }
            else if (buffer.IsSingleSegment)
            {
                // No request line end
                return false;
            }
            else if (TryGetNewLine(buffer, out var found))
            {
                span = buffer.Slice(consumed, found).ToSpan();
                consumed = found;
            }
            else
            {
                // No request line end
                return false;
            }

            // Fix and parse the span
            fixed (byte* data = span)
            {
                ParseRequestLine(handler, data, span.Length);
            }

            examined = consumed;
            return true;
        }

        private unsafe void ParseRequestLine(TRequestHandler handler, byte* data, int length)
        {
            // Get Method and set the offset
            var method = HttpUtilities.GetKnownMethod(data, length, out var pathStartOffset);

            Span<byte> customMethod = default;
            if (method == HttpMethod.Custom)
            {
                customMethod = GetUnknownMethod(data, length, out pathStartOffset);
            }

            // Use a new offset var as pathStartOffset needs to be on stack
            // as its passed by reference above so can't be in register.
            // Skip space
            var offset = pathStartOffset + 1;
            if (offset >= length)
            {
                // Start of path not found
                RejectRequestLine(data, length);
            }

            byte ch = data[offset];
            if (ch == ByteSpace || ch == ByteQuestionMark || ch == BytePercentage)
            {
                // Empty path is illegal, or path starting with percentage
                RejectRequestLine(data, length);
            }

            // Target = Path and Query
            var pathEncoded = false;
            var pathStart = offset;

            // Skip first char (just checked)
            offset++;

            // Find end of path and if path is encoded
            for (; offset < length; offset++)
            {
                ch = data[offset];
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

            var pathBuffer = new Span<byte>(data + pathStart, offset - pathStart);

            // Query string
            var queryStart = offset;
            if (ch == ByteQuestionMark)
            {
                // We have a query string
                for (; offset < length; offset++)
                {
                    ch = data[offset];
                    if (ch == ByteSpace)
                    {
                        break;
                    }
                }
            }

            // End of query string not found
            if (offset == length)
            {
                RejectRequestLine(data, length);
            }

            var targetBuffer = new Span<byte>(data + pathStart, offset - pathStart);
            var query = new Span<byte>(data + queryStart, offset - queryStart);

            // Consume space
            offset++;

            // Version
            var httpVersion = HttpUtilities.GetKnownVersion(data + offset, length - offset);
            if (httpVersion == HttpVersion.Unknown)
            {
                if (data[offset] == ByteCR || data[length - 2] != ByteCR)
                {
                    // If missing delimiter or CR before LF, reject and log entire line
                    RejectRequestLine(data, length);
                }
                else
                {
                    // else inform HTTP version is unsupported.
                    RejectUnknownVersion(data + offset, length - offset - 2);
                }
            }

            // After version's 8 bytes and CR, expect LF
            if (data[offset + 8 + 1] != ByteLF)
            {
                RejectRequestLine(data, length);
            }

            handler.OnStartLine(method, httpVersion, targetBuffer, pathBuffer, query, customMethod, pathEncoded);
        }

        public bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader)
        {
            while (!reader.End)
            {
                var span = reader.UnreadSpan;
                while (span.Length > 0)
                {
                    var ch1 = (byte)0;
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
                        length = span.IndexOfAny(ByteCR, ByteLF);
                        // If not found length with be -1; casting to uint will turn it to uint.MaxValue
                        // which will be larger than any possible span.Length. This also serves to eliminate
                        // the bounds check for the next lookup of span[length]
                        if ((uint)length < (uint)span.Length)
                        {
                            // Early memory read to hide latency
                            var expectedCR = span[length];
                            // Correctly has a CR, move to next
                            length++;

                            if (expectedCR != ByteCR)
                            {
                                // Sequence needs to be CRLF not LF first.
                                RejectRequestHeader(span[..length]);
                            }

                            if ((uint)length < (uint)span.Length)
                            {
                                // Early memory read to hide latency
                                var expectedLF = span[length];
                                // Correctly has a LF, move to next
                                length++;

                                if (expectedLF != ByteLF ||
                                    length < 5 ||
                                    // Exclude the CRLF from the headerLine and parse the header name:value pair
                                    !TryTakeSingleHeader(handler, span[..(length - 2)]))
                                {
                                    // Sequence needs to be CRLF and not contain an inner CR not part of terminator.
                                    // Less than min possible headerSpan of 5 bytes a:b\r\n
                                    // Not parsable as a valid name:value header pair.
                                    RejectRequestHeader(span[..length]);
                                }

                                // Read the header successfully, skip the reader forward past the headerSpan.
                                span = span.Slice(length);
                                reader.Advance(length);
                            }
                            else
                            {
                                // No enough data, set length to 0.
                                length = 0;
                            }
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
        private static bool TryGetNewLine(in ReadOnlySequence<byte> buffer, out SequencePosition found)
        {
            var byteLfPosition = buffer.PositionOf(ByteLF);
            if (byteLfPosition != null)
            {
                // Move 1 byte past the \n
                found = buffer.GetPosition(1, byteLfPosition.Value);
                return true;
            }

            found = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe Span<byte> GetUnknownMethod(byte* data, int length, out int methodLength)
        {
            var invalidIndex = HttpCharacters.IndexOfInvalidTokenChar(data, length);

            if (invalidIndex <= 0 || data[invalidIndex] != ByteSpace)
            {
                RejectRequestLine(data, length);
            }

            methodLength = invalidIndex;
            return new Span<byte>(data, methodLength);
        }

        private unsafe bool IsTlsHandshake(byte* data, int length)
        {
            const byte SslRecordTypeHandshake = (byte)0x16;

            // Make sure we can check at least for the existence of a TLS handshake - we check the first byte
            // See https://serializethoughts.com/2014/07/27/dissecting-tls-client-hello-message/

            return (length >= MinTlsRequestSize && data[0] == SslRecordTypeHandshake);
        }

        [StackTraceHidden]
        private unsafe void RejectRequestLine(byte* requestLine, int length)
        {
            // Check for incoming TLS handshake over HTTP
            if (IsTlsHandshake(requestLine, length))
            {
                throw GetInvalidRequestException(RequestRejectionReason.TlsOverHttpError, requestLine, length);
            }
            else
            {
                throw GetInvalidRequestException(RequestRejectionReason.InvalidRequestLine, requestLine, length);
            }
        }

        [StackTraceHidden]
        private void RejectRequestHeader(ReadOnlySpan<byte> headerLine)
            => throw GetInvalidRequestException(RequestRejectionReason.InvalidRequestHeader, headerLine);

        [StackTraceHidden]
        private unsafe void RejectUnknownVersion(byte* version, int length)
            => throw GetInvalidRequestException(RequestRejectionReason.UnrecognizedHTTPVersion, version, length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe BadHttpRequestException GetInvalidRequestException(RequestRejectionReason reason, byte* detail, int length)
            => GetInvalidRequestException(reason, new ReadOnlySpan<byte>(detail, length));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private BadHttpRequestException GetInvalidRequestException(RequestRejectionReason reason, ReadOnlySpan<byte> headerLine)
            => KestrelBadHttpRequestException.GetException(
                reason,
                _showErrorDetails
                    ? headerLine.GetAsciiStringEscaped(Constants.MaxExceptionDetailSize)
                    : string.Empty);
    }
}
