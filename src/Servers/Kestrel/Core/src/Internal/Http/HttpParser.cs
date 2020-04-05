// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
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
            var method = HttpUtilities.GetKnownMethod(data, length, out var methodEnd);
            if (method == HttpMethod.Custom)
            {
                methodEnd = GetUnknownMethodLength(data, length);
            }

            var versionAndMethod = new HttpVersionAndMethod(method, methodEnd);

            // Use a new offset var as methodEnd needs to be on stack
            // as its passed by reference above so can't be in register.
            // Skip space
            var offset = methodEnd + 1;
            if (offset >= length)
            {
                // Start of path not found
                RejectRequestLine(data, length);
            }

            var ch = data[offset];
            if (ch == ByteSpace || ch == ByteQuestionMark || ch == BytePercentage)
            {
                // Empty path is illegal, or path starting with percentage
                RejectRequestLine(data, length);
            }

            // Target = Path and Query
            var targetStart = offset;
            var pathEncoded = false;
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

            var path = new TargetOffsetPathLength(targetStart, length: offset - targetStart, pathEncoded);

            // Query string
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

            var queryEnd = offset;

            // Consume space
            offset++;

            // Version
            var httpVersion = HttpUtilities.GetKnownVersion(data + offset, length - offset);
            versionAndMethod.Version = httpVersion;
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

            var startLine = new Span<byte>(data, queryEnd);
            handler.OnStartLine(versionAndMethod, path, startLine);
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
                        BadHttpRequestException.Throw(RequestRejectionReason.InvalidRequestHeadersNoCRLF);
                    }

                    var length = 0;
                    // We only need to look for the end if we didn't read ahead; otherwise there isn't enough in
                    // in the span to contain a header.
                    if (readAhead == 0)
                    {
                        length = span.IndexOf(ByteLF) + 1;
                        if (length > 0)
                        {
                            if (length < 5 ||
                                span[(length - 2)] != ByteCR ||
                                !TryTakeSingleHeader(handler, span[..(length - 2)])) // Do not include CRLF
                            {
                                // Min is 5 chars a:b\r\n
                                // Last char is ByteLF and Second last char must be ByteCR
                                RejectRequestHeader(span[..length]);
                            }

                            // Read the header successfully, skip the reader forward past the header line.
                            reader.Advance(length);
                            span = span.Slice(length);
                        }
                    }

                    // End not found in current span
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
            var lineEndPosition = currentSlice.PositionOf(ByteLF);
            // Split buffers
            if (lineEndPosition == null)
            {
                // Not there
                return -1;
            }

            var lineEnd = lineEndPosition.Value;
            // Make sure LF is included in lineEnd
            lineEnd = currentSlice.GetPosition(1, lineEnd);
            var headerSpan = currentSlice.Slice(reader.Position, lineEnd).ToSpan();
            if (headerSpan.Length < 5 ||
                headerSpan[^2] != ByteCR ||
                !TryTakeSingleHeader(handler, headerSpan[..^2])) // Do not include CRLF
            {
                // Min is 4 chars a:b\r
                // Second last char must be ByteCR
                RejectRequestHeader(headerSpan);
            }

            // Include LF in length
            return headerSpan.Length;
        }

        private static bool TryTakeSingleHeader(TRequestHandler handler, ReadOnlySpan<byte> headerLine)
        {
            if (headerLine.Contains(ByteCR))
            {
                // Neither header name, nor header value can contain a CR.
                goto Reject;
            }

            // We are looking for a colon to terminate the header name.
            // However, the header name cannot contain a space or tab so look for all three
            // and see which is found first.
            var nameEnd = headerLine.IndexOfAny(ByteColon, ByteSpace, ByteTab);
            if (nameEnd <= 0 || headerLine[nameEnd] != ByteColon)
            {
                // Header name is empty or contains space or tab.
                goto Reject;
            }

            var headerName = headerLine[..nameEnd];
            // Skip colon to get to the value start.
            var valueStart = nameEnd + 1;
            // Ignore start whitespace. Generally there will only be one space
            // so we will just do a char by char loop inline.
            for (; valueStart < headerLine.Length; valueStart++)
            {
                var ch = headerLine[valueStart];
                if (ch != ByteTab && ch != ByteSpace)
                {
                    // Non-whitespace char found, valueStart is now start of value.
                    break;
                }
            }

            // Ignore end whitespace. Generally there will no spaces
            // so we will just do a char by char loop from end inline.
            var valueEnd = headerLine.Length - 1;
            for (; valueEnd >= valueStart; valueEnd--)
            {
                var ch = headerLine[valueEnd];
                if (ch != ByteTab && ch != ByteSpace)
                {
                    // Non-whitespace char found, valueEnd is now start of value.
                    break;
                }
            }

            // Range end is exclusive, so add 1 to valueEnd
            valueEnd++;
            var headerValue = headerLine[valueStart..valueEnd];

            handler.OnHeader(headerName, headerValue);
            return true;

        Reject:
            // Reject is a jump forward as we expect most headers to be accepted,
            // so we want it to be unpredicted by an unprimied branch predictor.
            return false;
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
        private unsafe int GetUnknownMethodLength(byte* data, int length)
        {
            var invalidIndex = HttpCharacters.IndexOfInvalidTokenChar(data, length);

            if (invalidIndex <= 0 || data[invalidIndex] != ByteSpace)
            {
                RejectRequestLine(data, length);
            }

            return invalidIndex;
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
            => BadHttpRequestException.GetException(
                reason,
                _showErrorDetails
                    ? new ReadOnlySpan<byte>(detail, length).GetAsciiStringEscaped(Constants.MaxExceptionDetailSize)
                    : string.Empty);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private BadHttpRequestException GetInvalidRequestException(RequestRejectionReason reason, ReadOnlySpan<byte> headerLine)
            => BadHttpRequestException.GetException(
                reason,
                _showErrorDetails
                    ? headerLine.GetAsciiStringEscaped(Constants.MaxExceptionDetailSize)
                    : string.Empty);
    }
}
