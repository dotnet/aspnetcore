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

        public bool ParseRequestLine(TRequestHandler handler, ref BufferReader<byte> reader)
        {
            // Look for CR/LF
            var startPosition = reader.Consumed;

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

        private void ParseRequestLine(TRequestHandler handler, in ReadOnlySpan<byte> data)
        {
            ReadOnlySpan<byte> customMethod = default;

            // Get Method and set the offset
            if (!HttpUtilities.GetKnownMethod(data, out HttpMethod method, out int offset))
            {
                customMethod = GetUnknownMethod(data, out offset);
            }

            // Skip the space after the method
            offset++;
            var target = data.Slice(offset);

            var pathEncoded = false;
            var pathEnd = target.IndexOfAny(ByteSpace, ByteQuestionMark);
            if (pathEnd < 1 || pathEnd > target.Length - 1)
            {
                // Cant start or end with space/? or eat the entire target
                RejectRequestLine(data);
            }

            var path = target.Slice(0, pathEnd);

            var escapeIndex = path.IndexOf(BytePercentage);
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
                var spaceIndex = query.IndexOf(ByteSpace);
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
            var version = data.Slice(offset + target.Length + 1);

            if (!HttpUtilities.GetKnownVersion(version, out HttpVersion httpVersion))
            {
                if (version.Length == 0)
                {
                    RejectRequestLine(data);
                }

                RejectUnknownVersion(version);
            }

            handler.OnStartLine(method, httpVersion, target, path, query, customMethod, pathEncoded);
        }

        public bool ParseHeaders(
            TRequestHandler handler,
            ref BufferReader<byte> reader)
        {
            bool success = false;

            while (!reader.End)
            {
                var consumed = reader.Consumed;

                if (!reader.TryReadToAny(out ReadOnlySpan<byte> headerLine, Eol, advancePastDelimiter: false))
                {
                    // Couldn't find another delimiter
                    break;
                }

                int headerLength = headerLine.Length;
                if (!reader.IsNext(Eol, advancePast: true))
                {
                    // Not a good CR/LF pair
                    RejectCRLF(ref reader, headerLine);
                    reader.Rewind(reader.Consumed - consumed);
                    break;
                }

                if (headerLength == 0)
                {
                    // Consider an empty line to be the end
                    success = true;
                    break;
                }

                TakeSingleHeader(headerLine, handler);
            }

            return success;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void RejectCRLF(ref BufferReader<byte> reader, ReadOnlySpan<byte> headerLine)
        {
            if (reader.TryRead(out byte value) && value == ByteCR && reader.End)
            {
                // Incomplete if ends in CR
                return;
            }

            if (headerLine.Length == 0 && value != ByteLF)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.InvalidRequestHeadersNoCRLF);
            }
            else
            {
                RejectRequestHeader(headerLine);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TakeSingleHeader<T>(ReadOnlySpan<byte> headerLine, T handler) where T : IHttpHeadersHandler
        {
            Debug.Assert(headerLine.IndexOf(ByteCR) == -1);

            // Find the end of the name (name:value)
            var nameEnd = 0;
            for (; nameEnd < headerLine.Length; nameEnd++)
            {
                var ch = headerLine[nameEnd];
                if (ch == ByteColon)
                {
                    break;
                }
                else if (ch == ByteTab || ch == ByteSpace)
                {
                    RejectRequestHeader(headerLine);
                }
            }

            if (nameEnd == 0 || nameEnd == headerLine.Length)
            {
                // Couldn't find the colon, or no name
                RejectRequestHeader(headerLine);
            }

            // Move past the colon
            var valueStart = nameEnd + 1;

            // Trim any whitespace from the start and end of the value
            for (; valueStart < headerLine.Length; valueStart++)
            {
                var ch = headerLine[valueStart];
                if (ch != ByteSpace && ch != ByteTab)
                {
                    break;
                }
            }

            var valueEnd = headerLine.Length - 1;
            for (; valueEnd > valueStart; valueEnd--)
            {
                byte ch = headerLine[valueEnd];
                if (ch != ByteSpace && ch != ByteTab)
                {
                    break;
                }
            }

            handler.OnHeader(
                headerLine.Slice(0, nameEnd),
                headerLine.Slice(valueStart, valueEnd - valueStart + 1));
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
