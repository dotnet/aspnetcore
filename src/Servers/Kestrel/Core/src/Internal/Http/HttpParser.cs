// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

        public unsafe bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader)
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
                            // Potentially found the end, or an invalid header.
                            fixed (byte* pHeader = span)
                            {
                                TakeSingleHeader(pHeader, length, handler);
                            }
                            // Read the header successfully, skip the reader forward past the header line.
                            reader.Advance(length);
                            span = span.Slice(length);
                        }
                    }

                    // End not found in current span
                    if (length <= 0)
                    {
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
            }

            return false;
        }

        private unsafe int ParseMultiSpanHeader(TRequestHandler handler, ref SequenceReader<byte> reader)
        {
            var buffer = reader.Sequence;
            var currentSlice = buffer.Slice(reader.Position, reader.Remaining);
            var lineEndPosition = currentSlice.PositionOf(ByteLF);
            // Split buffers
            if (lineEndPosition == null)
            {
                // Not there
                return -1;
            }

            var lineEnd = lineEndPosition.Value;

            // Make sure LF is included in lineEnd
            lineEnd = buffer.GetPosition(1, lineEnd);
            var headerSpan = buffer.Slice(reader.Position, lineEnd).ToSpan();
            var length = headerSpan.Length;

            fixed (byte* pHeader = headerSpan)
            {
                TakeSingleHeader(pHeader, length, handler);
            }

            return length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int FindEndOfName(byte* headerLine, int length)
        {
            var index = 0;
            var sawWhitespace = false;
            for (; index < length; index++)
            {
                var ch = headerLine[index];
                if (ch == ByteColon)
                {
                    break;
                }
                if (ch == ByteTab || ch == ByteSpace || ch == ByteCR)
                {
                    sawWhitespace = true;
                }
            }

            if (index == length || sawWhitespace)
            {
                // Set to -1 to indicate invalid.
                index = -1;
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void TakeSingleHeader(byte* headerLine, int length, TRequestHandler handler)
        {
            // Skip CR, LF from end position
            var valueEnd = length - 3;
            var nameEnd = FindEndOfName(headerLine, length);

            // Header name is empty, invalid, or doesn't end in CRLF
            if (nameEnd <= 0 || headerLine[valueEnd + 2] != ByteLF || headerLine[valueEnd + 1] != ByteCR)
            {
                RejectRequestHeader(headerLine, length);
            }

            // Skip colon from value start
            var valueStart = nameEnd + 1;
            // Ignore start whitespace
            for (; valueStart < valueEnd; valueStart++)
            {
                var ch = headerLine[valueStart];
                if (ch != ByteTab && ch != ByteSpace && ch != ByteCR)
                {
                    break;
                }
                else if (ch == ByteCR)
                {
                    RejectRequestHeader(headerLine, length);
                }
            }

            // Check for CR in value
            var valueBuffer = new Span<byte>(headerLine + valueStart, valueEnd - valueStart + 1);
            if (valueBuffer.Contains(ByteCR))
            {
                RejectRequestHeader(headerLine, length);
            }

            // Ignore end whitespace
            var lengthChanged = false;
            for (; valueEnd >= valueStart; valueEnd--)
            {
                var ch = headerLine[valueEnd];
                if (ch != ByteTab && ch != ByteSpace)
                {
                    break;
                }

                lengthChanged = true;
            }

            if (lengthChanged)
            {
                // Length changed
                valueBuffer = new Span<byte>(headerLine + valueStart, valueEnd - valueStart + 1);
            }

            var nameBuffer = new Span<byte>(headerLine, nameEnd);

            handler.OnHeader(nameBuffer, valueBuffer);
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
        private unsafe void RejectRequestHeader(byte* headerLine, int length)
            => throw GetInvalidRequestException(RequestRejectionReason.InvalidRequestHeader, headerLine, length);

        [StackTraceHidden]
        private unsafe void RejectUnknownVersion(byte* version, int length)
            => throw GetInvalidRequestException(RequestRejectionReason.UnrecognizedHTTPVersion, version, length);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe BadHttpRequestException GetInvalidRequestException(RequestRejectionReason reason, byte* detail, int length)
            => BadHttpRequestException.GetException(
                reason,
                _showErrorDetails
                    ? new Span<byte>(detail, length).GetAsciiStringEscaped(Constants.MaxExceptionDetailSize)
                    : string.Empty);
    }
}
