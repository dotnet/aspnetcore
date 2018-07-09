// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    public static partial class HttpUtilities
    {
        public const string Http10Version = "HTTP/1.0";
        public const string Http11Version = "HTTP/1.1";
        public const string Http2Version = "HTTP/2";

        public const string HttpUriScheme = "http://";
        public const string HttpsUriScheme = "https://";

        // readonly primitive statics can be Jit'd to consts https://github.com/dotnet/coreclr/issues/1079
        private static readonly ulong _httpSchemeLong = GetAsciiStringAsLong(HttpUriScheme + "\0");
        private static readonly ulong _httpsSchemeLong = GetAsciiStringAsLong(HttpsUriScheme);

        private const uint _httpGetMethodInt = 542393671; // GetAsciiStringAsInt("GET "); const results in better codegen

        private const ulong _http10VersionLong = 3471766442030158920; // GetAsciiStringAsLong("HTTP/1.0"); const results in better codegen
        private const ulong _http11VersionLong = 3543824036068086856; // GetAsciiStringAsLong("HTTP/1.1"); const results in better codegen

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetKnownMethod(ulong mask, ulong knownMethodUlong, HttpMethod knownMethod, int length)
        {
            _knownMethods[GetKnownMethodIndex(knownMethodUlong)] = new Tuple<ulong, ulong, HttpMethod, int>(mask, knownMethodUlong, knownMethod, length);
        }

        private static void FillKnownMethodsGaps()
        {
            var knownMethods = _knownMethods;
            var length = knownMethods.Length;
            var invalidHttpMethod = new Tuple<ulong, ulong, HttpMethod, int>(_mask8Chars, 0ul, HttpMethod.Custom, 0);
            for (int i = 0; i < length; i++)
            {
                if (knownMethods[i] == null)
                {
                    knownMethods[i] = invalidHttpMethod;
                }
            }
        }

        private static unsafe ulong GetAsciiStringAsLong(string str)
        {
            Debug.Assert(str.Length == 8, "String must be exactly 8 (ASCII) characters long.");

            var bytes = Encoding.ASCII.GetBytes(str);

            fixed (byte* ptr = &bytes[0])
            {
                return *(ulong*)ptr;
            }
        }

        private static unsafe uint GetAsciiStringAsInt(string str)
        {
            Debug.Assert(str.Length == 4, "String must be exactly 4 (ASCII) characters long.");

            var bytes = Encoding.ASCII.GetBytes(str);

            fixed (byte* ptr = &bytes[0])
            {
                return *(uint*)ptr;
            }
        }

        private static unsafe ulong GetMaskAsLong(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 8, "Mask must be exactly 8 bytes long.");

            fixed (byte* ptr = bytes)
            {
                return *(ulong*)ptr;
            }
        }

        public static unsafe string GetAsciiStringNonNullCharacters(this Span<byte> span)
        {
            if (span.IsEmpty)
            {
                return string.Empty;
            }

            var asciiString = new string('\0', span.Length);

            fixed (char* output = asciiString)
            fixed (byte* buffer = &MemoryMarshal.GetReference(span))
            {
                // This version if AsciiUtilities returns null if there are any null (0 byte) characters
                // in the string
                if (!StringUtilities.TryGetAsciiString(buffer, output, span.Length))
                {
                    throw new InvalidOperationException();
                }
            }
            return asciiString;
        }

        public static string GetAsciiStringEscaped(this Span<byte> span, int maxChars)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < Math.Min(span.Length, maxChars); i++)
            {
                var ch = span[i];
                sb.Append(ch < 0x20 || ch >= 0x7F ? $"\\x{ch:X2}" : ((char)ch).ToString());
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
        public static unsafe bool GetKnownMethod(this Span<byte> span, out HttpMethod method, out int length)
        {
            fixed (byte* data = &MemoryMarshal.GetReference(span))
            {
                method = GetKnownMethod(data, span.Length, out length);
                return method != HttpMethod.Custom;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe HttpMethod GetKnownMethod(byte* data, int length, out int methodLength)
        {
            methodLength = 0;
            if (length < sizeof(uint))
            {
                return HttpMethod.Custom;
            }
            else if (*(uint*)data == _httpGetMethodInt)
            {
                methodLength = 3;
                return HttpMethod.Get;
            }
            else if (length < sizeof(ulong))
            {
                return HttpMethod.Custom;
            }
            else
            {
                var value = *(ulong*)data;
                var key = GetKnownMethodIndex(value);
                var x = _knownMethods[key];

                if (x != null && (value & x.Item1) == x.Item2)
                {
                    methodLength = x.Item4;
                    return x.Item3;
                }
            }

            return HttpMethod.Custom;
        }

        /// <summary>
        /// Parses string <paramref name="value"/> for a known HTTP method.
        /// </summary>
        /// <remarks>
        /// A "known HTTP method" can be an HTTP method name defined in the HTTP/1.1 RFC.
        /// The Known Methods (CONNECT, DELETE, GET, HEAD, PATCH, POST, PUT, OPTIONS, TRACE)
        /// </remarks>
        /// <returns><see cref="HttpMethod"/></returns>
        public static HttpMethod GetKnownMethod(string value)
        {
            // Called by http/2
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var length = value.Length;
            if (length == 0)
            {
                throw new ArgumentException(nameof(value));
            }

            // Start with custom and assign if known method is found
            var method = HttpMethod.Custom;

            var firstChar = value[0];
            if (length == 3)
            {
                if (firstChar == 'G' && string.Equals(value, HttpMethods.Get, StringComparison.Ordinal))
                {
                    method = HttpMethod.Get;
                }
                else if (firstChar == 'P' && string.Equals(value, HttpMethods.Put, StringComparison.Ordinal))
                {
                    method = HttpMethod.Put;
                }
            }
            else if (length == 4)
            {
                if (firstChar == 'H' && string.Equals(value, HttpMethods.Head, StringComparison.Ordinal))
                {
                    method = HttpMethod.Head;
                }
                else if(firstChar == 'P' && string.Equals(value, HttpMethods.Post, StringComparison.Ordinal))
                {
                    method = HttpMethod.Post;
                }
            }
            else if (length == 5)
            {
                if (firstChar == 'T' && string.Equals(value, HttpMethods.Trace, StringComparison.Ordinal))
                {
                    method = HttpMethod.Trace;
                }
                else if(firstChar == 'P' && string.Equals(value, HttpMethods.Patch, StringComparison.Ordinal))
                {
                    method = HttpMethod.Patch;
                }
            }
            else if (length == 6)
            {
                if (firstChar == 'D' && string.Equals(value, HttpMethods.Delete, StringComparison.Ordinal))
                {
                    method = HttpMethod.Delete;
                }
            }
            else if (length == 7)
            {
                if (firstChar == 'C' && string.Equals(value, HttpMethods.Connect, StringComparison.Ordinal))
                {
                    method = HttpMethod.Connect;
                }
                else if (firstChar == 'O' && string.Equals(value, HttpMethods.Options, StringComparison.Ordinal))
                {
                    method = HttpMethod.Options;
                }
            }

            return method;
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
        public static unsafe bool GetKnownVersion(this Span<byte> span, out HttpVersion knownVersion, out byte length)
        {
            fixed (byte* data = &MemoryMarshal.GetReference(span))
            {
                knownVersion = GetKnownVersion(data, span.Length);
                if (knownVersion != HttpVersion.Unknown)
                {
                    length = sizeof(ulong);
                    return true;
                }

                length = 0;
                return false;
            }
        }

        /// <summary>
        /// Checks 9 bytes from <paramref name="location"/>  correspond to a known HTTP version.
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
        internal static unsafe HttpVersion GetKnownVersion(byte* location, int length)
        {
            HttpVersion knownVersion;
            var version = *(ulong*)location;
            if (length < sizeof(ulong) + 1 || location[sizeof(ulong)] != (byte)'\r')
            {
                knownVersion = HttpVersion.Unknown;
            }
            else if (version == _http11VersionLong)
            {
                knownVersion = HttpVersion.Http11;
            }
            else if (version == _http10VersionLong)
            {
                knownVersion = HttpVersion.Http10;
            }
            else
            {
                knownVersion = HttpVersion.Unknown;
            }

            return knownVersion;
        }

        /// <summary>
        /// Checks 8 bytes from <paramref name="span"/> that correspond to 'http://' or 'https://'
        /// </summary>
        /// <param name="span">The span</param>
        /// <param name="knownScheme">A reference to the known scheme, if the input matches any</param>
        /// <returns>True when memory starts with known http or https schema</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool GetKnownHttpScheme(this Span<byte> span, out HttpScheme knownScheme)
        {
            fixed (byte* data = &MemoryMarshal.GetReference(span))
            {
                return GetKnownHttpScheme(data, span.Length, out knownScheme);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool GetKnownHttpScheme(byte* location, int length, out HttpScheme knownScheme)
        {
            if (length >= sizeof(ulong))
            {
                var scheme = *(ulong*)location;
                if ((scheme & _mask7Chars) == _httpSchemeLong)
                {
                    knownScheme = HttpScheme.Http;
                    return true;
                }

                if (scheme == _httpsSchemeLong)
                {
                    knownScheme = HttpScheme.Https;
                    return true;
                }
            }
            knownScheme = HttpScheme.Unknown;
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

        public static string SchemeToString(HttpScheme scheme)
        {
            switch (scheme)
            {
                case HttpScheme.Http:
                    return HttpUriScheme;
                case HttpScheme.Https:
                    return HttpsUriScheme;
                default:
                    return null;
            }
        }

        public static void ValidateHostHeader(string hostText)
        {
            if (string.IsNullOrEmpty(hostText))
            {
                // The spec allows empty values
                return;
            }

            var firstChar = hostText[0];
            if (firstChar == '[')
            {
                // Tail call
                ValidateIPv6Host(hostText);
            }
            else
            {
                if (firstChar == ':')
                {
                    // Only a port
                    BadHttpRequestException.Throw(RequestRejectionReason.InvalidHostHeader, hostText);
                }

                var invalid = HttpCharacters.IndexOfInvalidHostChar(hostText);
                if (invalid >= 0)
                {
                    // Tail call
                    ValidateHostPort(hostText, invalid);
                }
            }
        }

        // The lead '[' was already checked
        private static void ValidateIPv6Host(string hostText)
        {
            for (var i = 1; i < hostText.Length; i++)
            {
                var ch = hostText[i];
                if (ch == ']')
                {
                    // [::1] is the shortest valid IPv6 host
                    if (i < 4)
                    {
                        BadHttpRequestException.Throw(RequestRejectionReason.InvalidHostHeader, hostText);
                    }
                    else if (i + 1 < hostText.Length)
                    {
                        // Tail call
                        ValidateHostPort(hostText, i + 1);
                    }
                    return;
                }

                if (!IsHex(ch) && ch != ':' && ch != '.')
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.InvalidHostHeader, hostText);
                }
            }

            // Must contain a ']'
            BadHttpRequestException.Throw(RequestRejectionReason.InvalidHostHeader, hostText);
        }

        private static void ValidateHostPort(string hostText, int offset)
        {
            var firstChar = hostText[offset];
            offset++;
            if (firstChar != ':' || offset == hostText.Length)
            {
                // Must have at least one number after the colon if present.
                BadHttpRequestException.Throw(RequestRejectionReason.InvalidHostHeader, hostText);
            }

            for (var i = offset; i < hostText.Length; i++)
            {
                if (!IsNumeric(hostText[i]))
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.InvalidHostHeader, hostText);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNumeric(char ch)
        {
            // '0' <= ch && ch <= '9'
            // (uint)(ch - '0') <= (uint)('9' - '0')

            // Subtract start of range '0'
            // Cast to uint to change negative numbers to large numbers
            // Check if less than 10 representing chars '0' - '9'
            return (uint)(ch - '0') < 10u;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsHex(char ch)
        {
            return IsNumeric(ch)
                // || ('a' <= ch && ch <= 'f')
                // || ('A' <= ch && ch <= 'F');

                // Lowercase indiscriminately (or with 32)
                // Subtract start of range 'a'
                // Cast to uint to change negative numbers to large numbers
                // Check if less than 6 representing chars 'a' - 'f'
                || (uint)((ch | 32) - 'a') < 6u;
        }
    }
}
