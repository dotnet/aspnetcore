// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public class KestrelHttpParser : IHttpParser
    {
        public KestrelHttpParser(IKestrelTrace log)
        {
            Log = log;
        }

        private IKestrelTrace Log { get; }

        // byte types don't have a data type annotation so we pre-cast them; to avoid in-place casts
        private const byte ByteCR = (byte)'\r';
        private const byte ByteLF = (byte)'\n';
        private const byte ByteColon = (byte)':';
        private const byte ByteSpace = (byte)' ';
        private const byte ByteTab = (byte)'\t';
        private const byte ByteQuestionMark = (byte)'?';
        private const byte BytePercentage = (byte)'%';

        public unsafe bool ParseRequestLine<T>(T handler, ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined) where T : IHttpRequestLineHandler
        {
            consumed = buffer.Start;
            examined = buffer.End;

            // Prepare the first span
            var span = buffer.First.Span;
            var lineIndex = span.IndexOfVectorized(ByteLF);
            if (lineIndex >= 0)
            {
                consumed = buffer.Move(consumed, lineIndex + 1);
                span = span.Slice(0, lineIndex + 1);
            }
            else if (buffer.IsSingleSpan || !TryGetNewLineSpan(ref buffer, ref span, out consumed))
            {
                // No request line end
                return false;
            }

            // Fix and parse the span
            fixed (byte* data = &span.DangerousGetPinnableReference())
            {
                ParseRequestLine(handler, data, span.Length);
            }

            examined = consumed;
            return true;
        }

        private unsafe void ParseRequestLine<T>(T handler, byte* data, int length) where T : IHttpRequestLineHandler
        {
            int offset;
            Span<byte> customMethod;
            // Get Method and set the offset
            var method = HttpUtilities.GetKnownMethod(data, length, out offset);
            if (method == HttpMethod.Custom)
            {
                customMethod = GetUnknownMethod(data, length, out offset);
            }

            // Skip space
            offset++;

            byte ch = 0;
            // Target = Path and Query
            var pathEncoded = false;
            var pathStart = -1;
            for (; offset < length; offset++)
            {
                ch = data[offset];
                if (ch == ByteSpace)
                {
                    if (pathStart == -1)
                    {
                        // Empty path is illegal
                        RejectRequestLine(data, length);
                    }

                    break;
                }
                else if (ch == ByteQuestionMark)
                {
                    if (pathStart == -1)
                    {
                        // Empty path is illegal
                        RejectRequestLine(data, length);
                    }

                    break;
                }
                else if (ch == BytePercentage)
                {
                    if (pathStart == -1)
                    {
                        // Path starting with % is illegal
                        RejectRequestLine(data, length);
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
                // End of path not found
                RejectRequestLine(data, length);
            }

            var pathBuffer = new Span<byte>(data + pathStart, offset - pathStart);

            var queryStart = offset;
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

            var targetBuffer = new Span<byte>(data + pathStart, offset - pathStart);
            var query = new Span<byte>(data + queryStart, offset - queryStart);

            // Consume space
            offset++;

            // Version
            var httpVersion = HttpUtilities.GetKnownVersion(data + offset, length - offset);
            if (httpVersion == HttpVersion.Unknown)
            {
                RejectUnknownVersion(data, length, offset);
            }

            //  After version 8 bytes and cr 1 byte, expect lf
            if (data[offset + 8 + 1] != ByteLF)
            {
                RejectRequestLine(data, length);
            }

            handler.OnStartLine(method, httpVersion, targetBuffer, pathBuffer, query, customMethod, pathEncoded);
        }

        public unsafe bool ParseHeaders<T>(T handler, ReadableBuffer buffer, out ReadCursor consumed, out ReadCursor examined, out int consumedBytes) where T : IHttpHeadersHandler
        {
            consumed = buffer.Start;
            examined = buffer.End;
            consumedBytes = 0;

            var bufferEnd = buffer.End;

            var reader = new ReadableBufferReader(buffer);
            var start = default(ReadableBufferReader);
            var done = false;

            try
            {
                while (!reader.End)
                {
                    var span = reader.Span;
                    var remaining = span.Length;

                    fixed (byte* pBuffer = &span.DangerousGetPinnableReference())
                    {
                        while (remaining > 0)
                        {
                            var index = reader.Index;
                            int ch1;
                            int ch2;

                            // Fast path, we're still looking at the same span
                            if (remaining >= 2)
                            {
                                ch1 = pBuffer[index];
                                ch2 = pBuffer[index + 1];
                            }
                            else
                            {
                                // Store the reader before we look ahead 2 bytes (probably straddling
                                // spans)
                                start = reader;

                                // Possibly split across spans
                                ch1 = reader.Take();
                                ch2 = reader.Take();
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
                                    if (index == reader.Index)
                                    {
                                        reader.Skip(2);
                                    }

                                    done = true;
                                    return true;
                                }

                                // Headers don't end in CRLF line.
                                RejectRequest(RequestRejectionReason.InvalidRequestHeadersNoCRLF);
                            }

                            // We moved the reader so look ahead 2 bytes so reset both the reader
                            // and the index
                            if (index != reader.Index)
                            {
                                reader = start;
                                index = reader.Index;
                            }

                            var endIndex = new Span<byte>(pBuffer + index, remaining).IndexOfVectorized(ByteLF);
                            var length = 0;

                            if (endIndex != -1)
                            {
                                length = endIndex + 1;
                                var pHeader = pBuffer + index;

                                TakeSingleHeader(pHeader, length, handler);
                            }
                            else
                            {
                                var current = reader.Cursor;

                                // Split buffers
                                if (ReadCursorOperations.Seek(current, bufferEnd, out var lineEnd, ByteLF) == -1)
                                {
                                    // Not there
                                    return false;
                                }

                                // Make sure LF is included in lineEnd
                                lineEnd = buffer.Move(lineEnd, 1);
                                var headerSpan = buffer.Slice(current, lineEnd).ToSpan();
                                length = headerSpan.Length;

                                fixed (byte* pHeader = &headerSpan.DangerousGetPinnableReference())
                                {
                                    TakeSingleHeader(pHeader, length, handler);
                                }

                                // We're going to the next span after this since we know we crossed spans here
                                // so mark the remaining as equal to the headerSpan so that we end up at 0
                                // on the next iteration
                                remaining = length;
                            }

                            // Skip the reader forward past the header line
                            reader.Skip(length);
                            remaining -= length;
                        }
                    }
                }

                return false;
            }
            finally
            {
                consumed = reader.Cursor;
                consumedBytes = reader.ConsumedBytes;

                if (done)
                {
                    examined = consumed;
                }
            }
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
                RejectRequestHeader(headerLine, length);
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void TakeSingleHeader<T>(byte* headerLine, int length, T handler) where T : IHttpHeadersHandler
        {
            // Skip CR, LF from end position
            var valueEnd = length - 3;
            var nameEnd = FindEndOfName(headerLine, length);

            if (headerLine[valueEnd + 2] != ByteLF)
            {
                RejectRequestHeader(headerLine, length);
            }
            if (headerLine[valueEnd + 1] != ByteCR)
            {
                RejectRequestHeader(headerLine, length);
            }

            // Skip colon from value start
            var valueStart = nameEnd + 1;
            // Ignore start whitespace
            for(; valueStart < valueEnd; valueStart++)
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
            var i = valueStart + 1;
            if (Contains(headerLine + i, valueEnd - i, ByteCR))
            {
                RejectRequestHeader(headerLine, length);
            }

            // Ignore end whitespace
            for (; valueEnd >= valueStart; valueEnd--)
            {
                var ch = headerLine[valueEnd];
                if (ch != ByteTab && ch != ByteSpace)
                {
                    break;
                }
            }

            var nameBuffer = new Span<byte>(headerLine, nameEnd);
            var valueBuffer = new Span<byte>(headerLine + valueStart, valueEnd - valueStart + 1);

            handler.OnHeader(nameBuffer, valueBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool Contains(byte* searchSpace, int length, byte value)
        {
            var i = 0;
            if (Vector.IsHardwareAccelerated)
            {
                // Check Vector lengths
                if (length - Vector<byte>.Count >= i)
                {
                    var vValue = GetVector(value);
                    do
                    {
                        if (!Vector<byte>.Zero.Equals(Vector.Equals(vValue, Unsafe.Read<Vector<byte>>(searchSpace + i))))
                        {
                            goto found;
                        }

                        i += Vector<byte>.Count;
                    } while (length - Vector<byte>.Count >= i);
                }
            }

            // Check remaining for CR
            for (; i <= length; i++)
            {
                var ch = searchSpace[i];
                if (ch == value)
                {
                    goto found;
                }
            }
            return false;
        found:
            return true;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool TryGetNewLineSpan(ref ReadableBuffer buffer, ref Span<byte> span, out ReadCursor end)
        {
            var start = buffer.Start;
            if (ReadCursorOperations.Seek(start, buffer.End, out end, ByteLF) != -1)
            {
                // Move 1 byte past the \n
                end = buffer.Move(end, 1);
                span = buffer.Slice(start, end).ToSpan();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe Span<byte> GetUnknownMethod(byte* data, int length, out int methodLength)
        {
            methodLength = 0;
            for (var i = 0; i < length; i++)
            {
                var ch = data[i];

                if (ch == ByteSpace)
                {
                    if (i == 0)
                    {
                        RejectRequestLine(data, length);
                    }

                    methodLength = i;
                    break;
                }
                else if (!IsValidTokenChar((char)ch))
                {
                    RejectRequestLine(data, length);
                }
            }

            return new Span<byte>(data, methodLength);
        }

        private static bool IsValidTokenChar(char c)
        {
            // Determines if a character is valid as a 'token' as defined in the
            // HTTP spec: https://tools.ietf.org/html/rfc7230#section-3.2.6
            return
                (c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                c == '!' ||
                c == '#' ||
                c == '$' ||
                c == '%' ||
                c == '&' ||
                c == '\'' ||
                c == '*' ||
                c == '+' ||
                c == '-' ||
                c == '.' ||
                c == '^' ||
                c == '_' ||
                c == '`' ||
                c == '|' ||
                c == '~';
        }

        public static void RejectRequest(RequestRejectionReason reason)
        {
            throw BadHttpRequestException.GetException(reason);
        }

        private unsafe void RejectUnknownVersion(byte* data, int length, int versionStart)
        {
            throw GetRejectUnknownVersion(data, length, versionStart);
        }

        private unsafe void RejectRequestLine(byte* data, int length)
        {
            throw GetRejectRequestLineException(new Span<byte>(data, length));
        }

        private void RejectRequestLine(Span<byte> span)
        {
            throw GetRejectRequestLineException(span);
        }

        private BadHttpRequestException GetRejectRequestLineException(Span<byte> span)
        {
            const int MaxRequestLineError = 32;
            return BadHttpRequestException.GetException(RequestRejectionReason.InvalidRequestLine,
                Log.IsEnabled(LogLevel.Information) ? span.GetAsciiStringEscaped(MaxRequestLineError) : string.Empty);
        }

        private unsafe BadHttpRequestException GetRejectUnknownVersion(byte* data, int length, int versionStart)
        {
            var span = new Span<byte>(data, length);
            length -= versionStart;
            for (var i = 0; i < length; i++)
            {
                var ch = span[i + versionStart];
                if (ch == ByteCR)
                {
                    if (i == 0)
                    {
                        return GetRejectRequestLineException(span);
                    }
                    else
                    {
                        return BadHttpRequestException.GetException(RequestRejectionReason.UnrecognizedHTTPVersion,
                            span.Slice(versionStart, i).GetAsciiStringEscaped(32));
                    }
                }
            }

            return GetRejectRequestLineException(span);
        }

        private unsafe void RejectRequestHeader(byte* headerLine, int length)
        {
            RejectRequestHeader(new Span<byte>(headerLine, length));
        }

        private void RejectRequestHeader(Span<byte> span)
        {
            throw GetRejectRequestHeaderException(span);
        }

        private BadHttpRequestException GetRejectRequestHeaderException(Span<byte> span)
        {
            const int MaxRequestHeaderError = 128;
            return BadHttpRequestException.GetException(RequestRejectionReason.InvalidRequestHeader,
                Log.IsEnabled(LogLevel.Information) ? span.GetAsciiStringEscaped(MaxRequestHeaderError) : string.Empty);
        }

        public void Reset()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<byte> GetVector(byte vectorByte)
        {
            // Vector<byte> .ctor doesn't become an intrinsic due to detection issue
            // However this does cause it to become an intrinsic (with additional multiply and reg->reg copy)
            // https://github.com/dotnet/coreclr/issues/7459#issuecomment-253965670
            return Vector.AsVectorByte(new Vector<uint>(vectorByte * 0x01010101u));
        }
    }
}
