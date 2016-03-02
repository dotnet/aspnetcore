// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Infrastructure
{
    public static class MemoryPoolIteratorExtensions
    {
        private static readonly Encoding _utf8 = Encoding.UTF8;

        public const string HttpConnectMethod = "CONNECT";
        public const string HttpDeleteMethod = "DELETE";
        public const string HttpGetMethod = "GET";
        public const string HttpHeadMethod = "HEAD";
        public const string HttpPatchMethod = "PATCH";
        public const string HttpPostMethod = "POST";
        public const string HttpPutMethod = "PUT";
        public const string HttpOptionsMethod = "OPTIONS";
        public const string HttpTraceMethod = "TRACE";

        public const string Http10Version = "HTTP/1.0";
        public const string Http11Version = "HTTP/1.1";

        // readonly primitive statics can be Jit'd to consts https://github.com/dotnet/coreclr/issues/1079
        private readonly static long _httpConnectMethodLong = GetAsciiStringAsLong("CONNECT ");
        private readonly static long _httpDeleteMethodLong = GetAsciiStringAsLong("DELETE \0");
        private readonly static long _httpGetMethodLong = GetAsciiStringAsLong("GET \0\0\0\0");
        private readonly static long _httpHeadMethodLong = GetAsciiStringAsLong("HEAD \0\0\0");
        private readonly static long _httpPatchMethodLong = GetAsciiStringAsLong("PATCH \0\0");
        private readonly static long _httpPostMethodLong = GetAsciiStringAsLong("POST \0\0\0");
        private readonly static long _httpPutMethodLong = GetAsciiStringAsLong("PUT \0\0\0\0");
        private readonly static long _httpOptionsMethodLong = GetAsciiStringAsLong("OPTIONS ");
        private readonly static long _httpTraceMethodLong = GetAsciiStringAsLong("TRACE \0\0");

        private readonly static long _http10VersionLong = GetAsciiStringAsLong("HTTP/1.0");
        private readonly static long _http11VersionLong = GetAsciiStringAsLong("HTTP/1.1");
 
        private readonly static long _mask8Chars = GetMaskAsLong(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff });
        private readonly static long _mask7Chars = GetMaskAsLong(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00 });
        private readonly static long _mask6Chars = GetMaskAsLong(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00 });
        private readonly static long _mask5Chars = GetMaskAsLong(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00 });
        private readonly static long _mask4Chars = GetMaskAsLong(new byte[] { 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00 });

        private readonly static Tuple<long, long, string>[] _knownMethods = new Tuple<long, long, string>[8];

        static MemoryPoolIteratorExtensions()
        {
            _knownMethods[0] = Tuple.Create(_mask4Chars, _httpPutMethodLong, HttpPutMethod);
            _knownMethods[1] = Tuple.Create(_mask5Chars, _httpPostMethodLong, HttpPostMethod);
            _knownMethods[2] = Tuple.Create(_mask5Chars, _httpHeadMethodLong, HttpHeadMethod);
            _knownMethods[3] = Tuple.Create(_mask6Chars, _httpTraceMethodLong, HttpTraceMethod);
            _knownMethods[4] = Tuple.Create(_mask6Chars, _httpPatchMethodLong, HttpPatchMethod);
            _knownMethods[5] = Tuple.Create(_mask7Chars, _httpDeleteMethodLong, HttpDeleteMethod);
            _knownMethods[6] = Tuple.Create(_mask8Chars, _httpConnectMethodLong, HttpConnectMethod);
            _knownMethods[7] = Tuple.Create(_mask8Chars, _httpOptionsMethodLong, HttpOptionsMethod);
        }

        private unsafe static long GetAsciiStringAsLong(string str)
        {
            Debug.Assert(str.Length == 8, "String must be exactly 8 (ASCII) characters long.");

            var bytes = Encoding.ASCII.GetBytes(str);

            fixed (byte* ptr = &bytes[0])
            {
                return *(long*)ptr;
            }
        }
        private unsafe static long GetMaskAsLong(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 8, "Mask must be exactly 8 bytes long.");

            fixed (byte* ptr = bytes)
            {
                return *(long*)ptr;
            }
        }

        public unsafe static string GetAsciiString(this MemoryPoolIterator start, MemoryPoolIterator end)
        {
            if (start.IsDefault || end.IsDefault)
            {
                return null;
            }

            var length = start.GetLength(end);

            if (length == 0)
            {
                return null;
            }

            // Bytes out of the range of ascii are treated as "opaque data" 
            // and kept in string as a char value that casts to same input byte value
            // https://tools.ietf.org/html/rfc7230#section-3.2.4

            var inputOffset = start.Index;
            var block = start.Block;

            var asciiString = new string('\0', length);

            fixed (char* outputStart = asciiString)
            {
                var output = outputStart;
                var remaining = length;

                var endBlock = end.Block;
                var endIndex = end.Index;

                while (true)
                {
                    int following = (block != endBlock ? block.End : endIndex) - inputOffset;

                    if (following > 0)
                    {
                        fixed (byte* blockStart = block.Array)
                        {
                            var input = blockStart + inputOffset;
                            var i = 0;
                            while (i < following - 11)
                            {
                                i += 12;
                                *(output) = (char)*(input);
                                *(output + 1) = (char)*(input + 1);
                                *(output + 2) = (char)*(input + 2);
                                *(output + 3) = (char)*(input + 3);
                                *(output + 4) = (char)*(input + 4);
                                *(output + 5) = (char)*(input + 5);
                                *(output + 6) = (char)*(input + 6);
                                *(output + 7) = (char)*(input + 7);
                                *(output + 8) = (char)*(input + 8);
                                *(output + 9) = (char)*(input + 9);
                                *(output + 10) = (char)*(input + 10);
                                *(output + 11) = (char)*(input + 11);
                                output += 12;
                                input += 12;
                            }
                            if (i < following - 5)
                            {
                                i += 6;
                                *(output) = (char)*(input);
                                *(output + 1) = (char)*(input + 1);
                                *(output + 2) = (char)*(input + 2);
                                *(output + 3) = (char)*(input + 3);
                                *(output + 4) = (char)*(input + 4);
                                *(output + 5) = (char)*(input + 5);
                                output += 6;
                                input += 6;
                            }
                            if (i < following - 3)
                            {
                                i += 4;
                                *(output) = (char)*(input);
                                *(output + 1) = (char)*(input + 1);
                                *(output + 2) = (char)*(input + 2);
                                *(output + 3) = (char)*(input + 3);
                                output += 4;
                                input += 4;
                            }
                            while (i < following)
                            {
                                i++;
                                *output = (char)*input;
                                output++;
                                input++;
                            }
                        
                            remaining -= following;
                        }
                    }

                    if (remaining == 0)
                    {
                        break;
                    }

                    block = block.Next;
                    inputOffset = block.Start;
                }
            }

            return asciiString;
        }

        public static string GetUtf8String(this MemoryPoolIterator start, MemoryPoolIterator end)
        {
            if (start.IsDefault || end.IsDefault)
            {
                return default(string);
            }
            if (end.Block == start.Block)
            {
                return _utf8.GetString(start.Block.Array, start.Index, end.Index - start.Index);
            }

            var decoder = _utf8.GetDecoder();

            var length = start.GetLength(end);
            var charLength = length * 2;
            var chars = new char[charLength];
            var charIndex = 0;

            var block = start.Block;
            var index = start.Index;
            var remaining = length;
            while (true)
            {
                int bytesUsed;
                int charsUsed;
                bool completed;
                var following = block.End - index;
                if (remaining <= following)
                {
                    decoder.Convert(
                        block.Array,
                        index,
                        remaining,
                        chars,
                        charIndex,
                        charLength - charIndex,
                        true,
                        out bytesUsed,
                        out charsUsed,
                        out completed);
                    return new string(chars, 0, charIndex + charsUsed);
                }
                else if (block.Next == null)
                {
                    decoder.Convert(
                        block.Array,
                        index,
                        following,
                        chars,
                        charIndex,
                        charLength - charIndex,
                        true,
                        out bytesUsed,
                        out charsUsed,
                        out completed);
                    return new string(chars, 0, charIndex + charsUsed);
                }
                else
                {
                    decoder.Convert(
                        block.Array,
                        index,
                        following,
                        chars,
                        charIndex,
                        charLength - charIndex,
                        false,
                        out bytesUsed,
                        out charsUsed,
                        out completed);
                    charIndex += charsUsed;
                    remaining -= following;
                    block = block.Next;
                    index = block.Start;
                }
            }
        }

        public static ArraySegment<byte> GetArraySegment(this MemoryPoolIterator start, MemoryPoolIterator end)
        {
            if (start.IsDefault || end.IsDefault)
            {
                return default(ArraySegment<byte>);
            }
            if (end.Block == start.Block)
            {
                return new ArraySegment<byte>(start.Block.Array, start.Index, end.Index - start.Index);
            }

            var length = start.GetLength(end);
            var array = new byte[length];
            start.CopyTo(array, 0, length, out length);
            return new ArraySegment<byte>(array, 0, length);
        }

        /// <summary>
        /// Checks that up to 8 bytes from <paramref name="begin"/> correspond to a known HTTP method.
        /// </summary>
        /// <remarks>
        /// A "known HTTP method" can be an HTTP method name defined in the HTTP/1.1 RFC.
        /// Since all of those fit in at most 8 bytes, they can be optimally looked up by reading those bytes as a long. Once
        /// in that format, it can be checked against the known method. 
        /// The Known Methods (CONNECT, DELETE, GET, HEAD, PATCH, POST, PUT, OPTIONS, TRACE) are all less than 8 bytes 
        /// and will be compared with the required space. A mask is used if the Known method is less than 8 bytes.
        /// To optimize performance the GET method will be checked first.
        /// </remarks>
        /// <param name="begin">The iterator from which to start the known string lookup.</param>
        /// <param name="scan">If we found a valid method, then scan will be updated to new position</param>
        /// <param name="knownMethod">A reference to a pre-allocated known string, if the input matches any.</param>
        /// <returns><c>true</c> if the input matches a known string, <c>false</c> otherwise.</returns>
        public static bool GetKnownMethod(this MemoryPoolIterator begin, ref MemoryPoolIterator scan, out string knownMethod)
        {
            knownMethod = null;
            var value = begin.PeekLong();

            if ((value & _mask4Chars) == _httpGetMethodLong)
            {
                knownMethod = HttpGetMethod;
                scan.Skip(4);
                return true;
            }
            foreach (var x in _knownMethods)
            {
                if ((value & x.Item1) == x.Item2)
                {
                    knownMethod = x.Item3;
                    scan.Skip(knownMethod.Length + 1);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks 9 bytes from <paramref name="begin"/>  correspond to a known HTTP version.
        /// </summary>
        /// <remarks>
        /// A "known HTTP version" Is is either HTTP/1.0 or HTTP/1.1.
        /// Since those fit in 8 bytes, they can be optimally looked up by reading those bytes as a long. Once
        /// in that format, it can be checked against the known versions.
        /// The Known versions will be checked with the required '\r'.
        /// To optimize performance the HTTP/1.1 will be checked first.
        /// </remarks>
        /// <param name="begin">The iterator from which to start the known string lookup.</param>
        /// <param name="scan">If we found a valid method, then scan will be updated to new position</param>
        /// <param name="knownVersion">A reference to a pre-allocated known string, if the input matches any.</param>
        /// <returns><c>true</c> if the input matches a known string, <c>false</c> otherwise.</returns>
        public static bool GetKnownVersion(this MemoryPoolIterator begin, ref MemoryPoolIterator scan, out string knownVersion)
        {
            knownVersion = null;
            var value = begin.PeekLong();

            if (value == _http11VersionLong)
            {
                knownVersion = Http11Version;
                scan.Skip(8);
                if (scan.Take() == '\r')
                {
                    return true;
                }
            }
            else if (value == _http10VersionLong)
            {
                knownVersion = Http10Version;
                scan.Skip(8);
                if (scan.Take() == '\r')
                {
                    return true;
                }
            }

            knownVersion = null;
            return false;
        }
    }
}
