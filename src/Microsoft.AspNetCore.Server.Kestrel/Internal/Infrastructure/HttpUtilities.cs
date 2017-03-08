// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure
{
    public static partial class HttpUtilities
    {
        public const string Http10Version = "HTTP/1.0";
        public const string Http11Version = "HTTP/1.1";
        private const uint _httpGetMethodInt = 542393671; // retun of GetAsciiStringAsInt("GET "); const results in better codegen


        private const ulong _http10VersionLong = 3471766442030158920; // GetAsciiStringAsLong("HTTP/1.0"); const results in better codegen
        private const ulong _http11VersionLong = 3543824036068086856; // GetAsciiStringAsLong("HTTP/1.1"); const results in better codegen


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetKnownMethod(ulong mask, ulong knownMethodUlong, HttpMethod knownMethod, int length)
        {
            _knownMethods[GetKnownMethodIndex(knownMethodUlong)] = new Tuple<ulong, ulong, HttpMethod, int, bool>(mask, knownMethodUlong, knownMethod, length, true);
        }

        private unsafe static ulong GetMaskAsLong(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 8, "Mask must be exactly 8 bytes long.");

            fixed (byte* ptr = bytes)
            {
                return *(ulong*)ptr;
            }
        }

        private static void FillKnownMethodsGaps()
        {
            var knownMethods = _knownMethods;
            var length = knownMethods.Length;
            var invalidHttpMethod = new Tuple<ulong, ulong, HttpMethod, int, bool>(_mask8Chars, 0ul, HttpMethod.Custom, 0, false);
            for (int i = 0; i < length; i++)
            {
                if (knownMethods[i] == null)
                {
                    knownMethods[i] = invalidHttpMethod;
                }
            }
        }

        private unsafe static ulong GetAsciiStringAsLong(string str)
        {
            Debug.Assert(str.Length == 8, "String must be exactly 8 (ASCII) characters long.");

            var bytes = Encoding.ASCII.GetBytes(str);

            fixed (byte* ptr = &bytes[0])
            {
                return *(ulong*)ptr;
            }
        }

        private unsafe static uint GetAsciiStringAsInt(string str)
        {
            Debug.Assert(str.Length == 4, "String must be exactly 4 (ASCII) characters long.");

            var bytes = Encoding.ASCII.GetBytes(str);

            fixed (byte* ptr = &bytes[0])
            {
                return *(uint*)ptr;
            }
        }



        public unsafe static string GetAsciiStringNonNullCharacters(this Span<byte> span)
        {
            if (span.IsEmpty)
            {
                return string.Empty;
            }

            var asciiString = new string('\0', span.Length);

            fixed (char* output = asciiString)
            fixed (byte* buffer = &span.DangerousGetPinnableReference())
            {
                // This version if AsciiUtilities returns null if there are any null (0 byte) characters
                // in the string
                if (!AsciiUtilities.TryGetAsciiString(buffer, output, span.Length))
                {
                    throw new InvalidOperationException();
                }
            }
            return asciiString;
        }

        public static string GetAsciiStringEscaped(this Span<byte> span, int maxChars)
        {
            var sb = new StringBuilder();

            int i;
            for (i = 0; i < Math.Min(span.Length, maxChars); ++i)
            {
                var ch = span[i];
                sb.Append(ch < 0x20 || ch >= 0x7F ? $"<0x{ch:X2}>" : ((char)ch).ToString());
            }

            if (span.Length > maxChars)
            {
                sb.Append("...");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Checks that up to 8 bytes from <paramref name="span"/> correspond to a known HTTP method.
        /// </summary>
        /// <remarks>
        /// A "known HTTP method" can be an HTTP method name defined in the HTTP/1.1 RFC.
        /// Since all of those fit in at most 8 bytes, they can be optimally looked up by reading those bytes as a long. Once
        /// in that format, it can be checked against the known method.
        /// The Known Methods (CONNECT, DELETE, GET, HEAD, PATCH, POST, PUT, OPTIONS, TRACE) are all less than 8 bytes
        /// and will be compared with the required space. A mask is used if the Known method is less than 8 bytes.
        /// To optimize performance the GET method will be checked first.
        /// </remarks>
        /// <returns><c>true</c> if the input matches a known string, <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetKnownMethod(this Span<byte> span, out HttpMethod method, out int length)
        {
            if (span.TryRead<uint>(out var possiblyGet))
            {
                if (possiblyGet == _httpGetMethodInt)
                {
                    length = 3;
                    method = HttpMethod.Get;
                    return true;
                }
            }

            if (span.TryRead<ulong>(out var value))
            {
                var key = GetKnownMethodIndex(value);

                var x = _knownMethods[key];

                if (x != null && (value & x.Item1) == x.Item2)
                {
                    method = x.Item3;
                    length = x.Item4;
                    return x.Item5;
                }
            }

            method = HttpMethod.Custom;
            length = 0;
            return false;
        }

        /// <summary>
        /// Checks 9 bytes from <paramref name="span"/>  correspond to a known HTTP version.
        /// </summary>
        /// <remarks>
        /// A "known HTTP version" Is is either HTTP/1.0 or HTTP/1.1.
        /// Since those fit in 8 bytes, they can be optimally looked up by reading those bytes as a long. Once
        /// in that format, it can be checked against the known versions.
        /// The Known versions will be checked with the required '\r'.
        /// To optimize performance the HTTP/1.1 will be checked first.
        /// </remarks>
        /// <returns><c>true</c> if the input matches a known string, <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetKnownVersion(this Span<byte> span, out HttpVersion knownVersion, out byte length)
        {
            if (span.TryRead<ulong>(out var version))
            {
                if (version == _http11VersionLong)
                {
                    length = sizeof(ulong);
                    knownVersion = HttpVersion.Http11;
                }
                else if (version == _http10VersionLong)
                {
                    length = sizeof(ulong);
                    knownVersion = HttpVersion.Http10;
                }
                else
                {
                    length = 0;
                    knownVersion = HttpVersion.Unknown;
                    return false;
                }

                if (span[sizeof(ulong)] == (byte)'\r')
                {
                    return true;
                }
            }

            knownVersion = HttpVersion.Unknown;
            length = 0;
            return false;
        }

        public static string VersionToString(HttpVersion httpVersion)
        {
            switch (httpVersion)
            {
                case HttpVersion.Http10:
                    return Http10Version;
                case HttpVersion.Http11:
                    return Http11Version;
                default:
                    return null;
            }
        }
        public static string MethodToString(HttpMethod method)
        {
            int methodIndex = (int)method;
            if (methodIndex >= 0 && methodIndex <= 8)
            {
                return _methodNames[methodIndex];
            }
            return null;
        }
    }
}
