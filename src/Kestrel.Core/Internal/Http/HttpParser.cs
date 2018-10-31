// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
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

        private static ReadOnlySpan<byte> Eol => new byte[] { ByteCR, ByteLF };

        public bool ParseRequestLine(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;
            examined = buffer.End;
            var reader = new BufferReader<byte>(buffer);
            bool success = ParseRequestLine(handler, ref reader);
            consumed = reader.Position;
            if (success) examined = reader.Position;
            return success;
        }

        public bool ParseRequestLine(TRequestHandler handler, ref BufferReader<byte> reader)
        {
            // Look for CR/LF
            long startPosition = reader.Consumed;

            if (!reader.TryReadToAny(out ReadOnlySpan<byte> requestLine, Eol, advancePastDelimiter: false))
            {
                // Couldn't find a delimiter
                return false;
            }

            if (!reader.IsNext(Eol, advancePast: true))
            {
                if (reader.TryRead(out byte value) && value == ByteCR && !reader.TryRead(out value))
                {
                    reader.Rewind(reader.Consumed - startPosition);

                    // Incomplete if ends in CR
                    return false;
                }

                // Not CR/LF
                RejectRequestLine(requestLine);
            }

            ParseRequestLine(handler, requestLine);
            return true;
        }

        private unsafe static string GETMESTRING(ReadOnlySpan<byte> span)
        {
            fixed (byte* b = span)
            {
                return Encoding.UTF8.GetString(b, span.Length);
            }
        }

        private void ParseRequestLine(TRequestHandler handler, in ReadOnlySpan<byte> data)
        {
            // TODO:
            // We can simplify GetKnownMethod if we take into account that the
            // absolute smallest request line is something like "Z * HTTP/1.1"
            // See https://www.w3.org/Protocols/rfc2616/rfc2616-sec5.html#sec5.1.2

            ReadOnlySpan<byte> customMethod = default;

            // Get Method and set the offset
            if (!HttpUtilities.GetKnownMethod(data, out HttpMethod method, out int offset))
            {
                customMethod = GetUnknownMethod(data, out offset);
            }

            // Skip the space after the method
            ReadOnlySpan<byte> target = data.Slice(++offset);

            bool pathEncoded = false;
            int pathEnd = target.IndexOfAny(ByteSpace, ByteQuestionMark);
            if (pathEnd < 1 || pathEnd > target.Length - 1)
            {
                // Cant start or end with space/? or eat the entire target
                RejectRequestLine(data);
            }

            ReadOnlySpan<byte> path = target.Slice(0, pathEnd);

            int escapeIndex = path.IndexOf(BytePercentage);
            if (escapeIndex == 0)
            {
                // Can't start with %
                RejectRequestLine(data);
            }
            else if (escapeIndex > 0)
            {
                pathEncoded = true;
            }

            ReadOnlySpan<byte> query = default;
            if (target[pathEnd] == ByteQuestionMark)
            {
                // Query string
                query = target.Slice(path.Length);
                int spaceIndex = query.IndexOf(ByteSpace);
                if (spaceIndex < 1)
                {
                    // End of query string not found
                    RejectRequestLine(data);
                }
                query = query.Slice(0, spaceIndex);
            }

            target = target.Slice(0, path.Length + query.Length);

            // Version

            // Skip space
            ReadOnlySpan<byte> version = data.Slice(offset + target.Length + 1);

            if (!HttpUtilities.GetKnownVersion(version, out HttpVersion httpVersion))
            {
                if (version.Length == 0)
                {
                    RejectRequestLine(data);
                }

                RejectUnknownVersion(version);
            }

            handler.OnStartLine(method, httpVersion, target.UnsafeAsSpan(), path.UnsafeAsSpan(), query.UnsafeAsSpan(), customMethod.UnsafeAsSpan(), pathEncoded);
        }

        public bool ParseHeaders(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined, out int consumedBytes)
        {
            consumed = buffer.Start;
            examined = buffer.End;
            consumedBytes = 0;

            var bufferEnd = buffer.End;

            var reader = new BufferReader<byte>(buffer);
            var start = default(BufferReader<byte>);
            var done = false;

            try
            {
                while (!reader.End)
                {
                    var span = reader.CurrentSpan;
                    var remaining = span.Length - reader.CurrentSpanIndex;

                    while (remaining > 0)
                    {
                        var index = reader.CurrentSpanIndex;
                        byte ch1;
                        byte ch2;
                        bool readSecond = false;
                        var readAhead = false;

                        // Fast path, we're still looking at the same span
                        if (remaining >= 2)
                        {
                            ch1 = span[index];
                            ch2 = span[index + 1];
                            readSecond = true;
                        }
                        else
                        {
                            // Store the reader before we look ahead 2 bytes (probably straddling
                            // spans)
                            start = reader;

                            // Possibly split across spans
                            reader.TryRead(out ch1);
                            readSecond = reader.TryRead(out ch2);

                            readAhead = true;
                        }

                        if (ch1 == ByteCR)
                        {
                            // Check for final CRLF.
                            if (!readSecond)
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
                            index = reader.CurrentSpanIndex;
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
                consumedBytes = checked((int)reader.Consumed);

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
