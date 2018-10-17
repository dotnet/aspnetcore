// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public class HttpParser<TRequestHandler> : IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler
    {
        private bool _showErrorDetails;

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

        public unsafe bool ParseRequestLine(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;
            examined = buffer.End;

            // Prepare the first span
            var span = buffer.First.Span;
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

            // Parse the span
            ParseRequestLine(handler, span);

            examined = consumed;
            return true;
        }

        private void ParseRequestLine(TRequestHandler handler, ReadOnlySpan<byte> span)
        {
            // Get Method and set the offset
            ReadOnlySpan<byte> customMethod = default;
            if (!HttpUtilities.GetKnownMethod(span, out HttpMethod method, out var offset))
            {
                customMethod = GetUnknownMethod(span, out offset);
            }

            // Skip space
            offset++;

            byte ch = 0;
            // Target = Path and Query
            var pathEncoded = false;
            var pathStart = -1;
            for (; offset < span.Length; offset++)
            {
                ch = span[offset];
                if (ch == ByteSpace)
                {
                    if (pathStart == -1)
                    {
                        // Empty path is illegal
                        RejectRequestLine(span);
                    }

                    break;
                }
                else if (ch == ByteQuestionMark)
                {
                    if (pathStart == -1)
                    {
                        // Empty path is illegal
                        RejectRequestLine(span);
                    }

                    break;
                }
                else if (ch == BytePercentage)
                {
                    if (pathStart == -1)
                    {
                        // Path starting with % is illegal
                        RejectRequestLine(span);
                    }

                    pathEncoded = true;
                }
                else if (pathStart == -1)
                {
                    pathStart = offset;
                }
            }

            if (pathStart == -1)
            {
                // Start of path not found
                RejectRequestLine(span);
            }

            var pathBuffer = span.Slice(pathStart, offset - pathStart);

            // Query string
            var queryStart = offset;
            if (ch == ByteQuestionMark)
            {
                // We have a query string
                for (; offset < span.Length; offset++)
                {
                    ch = span[offset];
                    if (ch == ByteSpace)
                    {
                        break;
                    }
                }
            }

            // End of query string not found
            if (offset == span.Length)
            {
                RejectRequestLine(span);
            }

            var targetBuffer = span.Slice(pathStart, offset - pathStart);
            var query = span.Slice(queryStart, offset - queryStart);

            // Consume space
            offset++;

            // Version
            if (!HttpUtilities.GetKnownVersion(span.Slice(offset), out HttpVersion httpVersion))
            {
                if (span[offset] == ByteCR || span[span.Length - 2] != ByteCR)
                {
                    // If missing delimiter or CR before LF, reject and log entire line
                    RejectRequestLine(span);
                }
                else
                {
                    // else inform HTTP version is unsupported.
                    RejectUnknownVersion(span.Slice(offset, span.Length - offset - 2));
                }
            }

            // After version's 8 bytes and CR, expect LF
            if (span[offset + 8 + 1] != ByteLF)
            {
                RejectRequestLine(span);
            }

            handler.OnStartLine(method, httpVersion, targetBuffer.UnsafeAsSpan(), pathBuffer.UnsafeAsSpan(), query.UnsafeAsSpan(), customMethod.UnsafeAsSpan(), pathEncoded);
        }

        public bool ParseHeaders(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined, out int consumedBytes)
        {
            consumed = buffer.Start;
            examined = buffer.End;
            consumedBytes = 0;

            var bufferEnd = buffer.End;

            var reader = new BufferReader(buffer);
            var start = default(BufferReader);
            var done = false;

            try
            {
                while (!reader.End)
                {
                    var span = reader.CurrentSegment;
                    var remaining = span.Length - reader.CurrentSegmentIndex;

                    while (remaining > 0)
                    {
                        var index = reader.CurrentSegmentIndex;
                        int ch1;
                        int ch2;
                        var readAhead = false;

                        // Fast path, we're still looking at the same span
                        if (remaining >= 2)
                        {
                            ch1 = span[index];
                            ch2 = span[index + 1];
                        }
                        else
                        {
                            // Store the reader before we look ahead 2 bytes (probably straddling
                            // spans)
                            start = reader;

                            // Possibly split across spans
                            ch1 = reader.Read();
                            ch2 = reader.Read();

                            readAhead = true;
                        }

                        if (ch1 == ByteCR)
                        {
                            // Check for final CRLF.
                            if (ch2 == -1)
                            {
                                // Reset the reader so we don't consume anything
                                reader = start;
                                return false;
                            }
                            else if (ch2 == ByteLF)
                            {
                                // If we got 2 bytes from the span directly so skip ahead 2 so that
                                // the reader's state matches what we expect
                                if (!readAhead)
                                {
                                    reader.Advance(2);
                                }

                                done = true;
                                return true;
                            }

                            // Headers don't end in CRLF line.
                            BadHttpRequestException.Throw(RequestRejectionReason.InvalidRequestHeadersNoCRLF);
                        }

                        // We moved the reader so look ahead 2 bytes so reset both the reader
                        // and the index
                        if (readAhead)
                        {
                            reader = start;
                            index = reader.CurrentSegmentIndex;
                        }

                        var endIndex = span.Slice(index, remaining).IndexOf(ByteLF);
                        var length = 0;

                        if (endIndex != -1)
                        {
                            length = endIndex + 1;
                            TakeSingleHeader(span.Slice(index, length), handler);
                        }
                        else
                        {
                            var current = reader.Position;
                            var currentSlice = buffer.Slice(current, bufferEnd);

                            var lineEndPosition = currentSlice.PositionOf(ByteLF);
                            // Split buffers
                            if (lineEndPosition == null)
                            {
                                // Not there
                                return false;
                            }

                            var lineEnd = lineEndPosition.Value;

                            // Make sure LF is included in lineEnd
                            lineEnd = buffer.GetPosition(1, lineEnd);
                            var headerSpan = buffer.Slice(current, lineEnd).ToSpan();
                            length = headerSpan.Length;
                            TakeSingleHeader(headerSpan, handler);

                            // We're going to the next span after this since we know we crossed spans here
                            // so mark the remaining as equal to the headerSpan so that we end up at 0
                            // on the next iteration
                            remaining = length;
                        }

                        // Skip the reader forward past the header line
                        reader.Advance(length);
                        remaining -= length;
                    }
                }

                return false;
            }
            finally
            {
                consumed = reader.Position;
                consumedBytes = reader.ConsumedBytes;

                if (done)
                {
                    examined = consumed;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEndOfName(ReadOnlySpan<byte> headerLine)
        {
            var index = 0;
            var sawWhitespace = false;
            for (; index < headerLine.Length; index++)
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

            if (index == headerLine.Length || sawWhitespace)
            {
                RejectRequestHeader(headerLine);
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TakeSingleHeader(ReadOnlySpan<byte> headerLine, TRequestHandler handler)
        {
            // Skip CR, LF from end position
            var valueEnd = headerLine.Length - 3;
            var nameEnd = FindEndOfName(headerLine);

            // Header name is empty
            if (nameEnd == 0)
            {
                RejectRequestHeader(headerLine);
            }

            if (headerLine[valueEnd + 2] != ByteLF)
            {
                RejectRequestHeader(headerLine);
            }
            if (headerLine[valueEnd + 1] != ByteCR)
            {
                RejectRequestHeader(headerLine);
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
                    RejectRequestHeader(headerLine);
                }
            }

            // Check for CR in value
            var valueBuffer = headerLine.Slice(valueStart, valueEnd - valueStart + 1);
            if (valueBuffer.IndexOf(ByteCR) >= 0)
            {
                RejectRequestHeader(headerLine);
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
                valueBuffer = headerLine.Slice(valueStart, valueEnd - valueStart + 1);
            }

            var nameBuffer = headerLine.Slice(0, nameEnd);

            handler.OnHeader(nameBuffer.UnsafeAsSpan(), valueBuffer.UnsafeAsSpan());
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
        private ReadOnlySpan<byte> GetUnknownMethod(ReadOnlySpan<byte> span, out int methodLength)
        {
            var invalidIndex = HttpCharacters.IndexOfInvalidTokenChar(span);

            if (invalidIndex <= 0 || span[invalidIndex] != ByteSpace)
            {
                RejectRequestLine(span);
            }

            methodLength = invalidIndex;
            return span.Slice(0, methodLength);
        }

        [StackTraceHidden]
        private void RejectRequestLine(ReadOnlySpan<byte> requestLine)
            => throw GetInvalidRequestException(RequestRejectionReason.InvalidRequestLine, requestLine);

        [StackTraceHidden]
        private void RejectRequestHeader(ReadOnlySpan<byte> headerLine)
            => throw GetInvalidRequestException(RequestRejectionReason.InvalidRequestHeader, headerLine);

        [StackTraceHidden]
        private void RejectUnknownVersion(ReadOnlySpan<byte> version)
            => throw GetInvalidRequestException(RequestRejectionReason.UnrecognizedHTTPVersion, version);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private BadHttpRequestException GetInvalidRequestException(RequestRejectionReason reason, ReadOnlySpan<byte> detail)
            => BadHttpRequestException.GetException(
                reason,
                _showErrorDetails
                    ? detail.GetAsciiStringEscaped(Constants.MaxExceptionDetailSize)
                    : string.Empty);
    }
}
