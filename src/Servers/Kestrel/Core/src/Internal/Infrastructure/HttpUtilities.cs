// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal static partial class HttpUtilities
    {
        public const string HttpUriScheme = "http://";
        public const string HttpsUriScheme = "https://";

        // readonly primitive statics can be Jit'd to consts https://github.com/dotnet/coreclr/issues/1079
        private static readonly ulong _httpSchemeLong = GetAsciiStringAsLong(HttpUriScheme + "\0");
        private static readonly ulong _httpsSchemeLong = GetAsciiStringAsLong(HttpsUriScheme);

        private const uint _httpGetMethodInt = 542393671; // GetAsciiStringAsInt("GET "); const results in better codegen

        private const ulong _http10VersionLong = 3471766442030158920; // GetAsciiStringAsLong("HTTP/1.0"); const results in better codegen
        private const ulong _http11VersionLong = 3543824036068086856; // GetAsciiStringAsLong("HTTP/1.1"); const results in better codegen

        private static readonly UTF8EncodingSealed DefaultRequestHeaderEncoding = new UTF8EncodingSealed();
        private static readonly SpanAction<char, IntPtr> _getHeaderName = GetHeaderName;
        private static readonly SpanAction<char, IntPtr> _getAsciiStringNonNullCharacters = GetAsciiStringNonNullCharacters;

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

        private static ulong GetAsciiStringAsLong(string str)
        {
            Debug.Assert(str.Length == 8, "String must be exactly 8 (ASCII) characters long.");

            var bytes = Encoding.ASCII.GetBytes(str);

            return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        }

        private static uint GetAsciiStringAsInt(string str)
        {
            Debug.Assert(str.Length == 4, "String must be exactly 4 (ASCII) characters long.");

            var bytes = Encoding.ASCII.GetBytes(str);
            return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        }

        private static ulong GetMaskAsLong(byte[] bytes)
        {
            Debug.Assert(bytes.Length == 8, "Mask must be exactly 8 bytes long.");

            return BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        }

        // The same as GetAsciiStringNonNullCharacters but throws BadRequest
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe string GetHeaderName(this ReadOnlySpan<byte> span)
        {
            if (span.IsEmpty)
            {
                return string.Empty;
            }

            fixed (byte* source = &MemoryMarshal.GetReference(span))
            {
                return string.Create(span.Length, new IntPtr(source), _getHeaderName);
            }
        }

        private static unsafe void GetHeaderName(Span<char> buffer, IntPtr state)
        {
            fixed (char* output = &MemoryMarshal.GetReference(buffer))
            {
                // This version if AsciiUtilities returns null if there are any null (0 byte) characters
                // in the string
                if (!StringUtilities.TryGetAsciiString((byte*)state.ToPointer(), output, buffer.Length))
                {
                    KestrelBadHttpRequestException.Throw(RequestRejectionReason.InvalidCharactersInHeaderName);
                }
            }
        }

        public static string GetAsciiStringNonNullCharacters(this Span<byte> span)
            => GetAsciiStringNonNullCharacters((ReadOnlySpan<byte>)span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe string GetAsciiStringNonNullCharacters(this ReadOnlySpan<byte> span)
        {
            if (span.IsEmpty)
            {
                return string.Empty;
            }

            fixed (byte* source = &MemoryMarshal.GetReference(span))
            {
                return string.Create(span.Length, new IntPtr(source), _getAsciiStringNonNullCharacters);
            }
        }

        public static string GetAsciiOrUTF8StringNonNullCharacters(this ReadOnlySpan<byte> span)
            => StringUtilities.GetAsciiOrUTF8StringNonNullCharacters(span, DefaultRequestHeaderEncoding);

        private static unsafe void GetAsciiStringNonNullCharacters(Span<char> buffer, IntPtr state)
        {
            fixed (char* output = &MemoryMarshal.GetReference(buffer))
            {
                // StringUtilities.TryGetAsciiString returns null if there are any null (0 byte) characters
                // in the string
                if (!StringUtilities.TryGetAsciiString((byte*)state.ToPointer(), output, buffer.Length))
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public static string GetRequestHeaderString(this ReadOnlySpan<byte> span, string name, Func<string, Encoding> encodingSelector)
        {
            if (ReferenceEquals(KestrelServerOptions.DefaultRequestHeaderEncodingSelector, encodingSelector))
            {
                return span.GetAsciiOrUTF8StringNonNullCharacters(DefaultRequestHeaderEncoding);
            }

            var encoding = encodingSelector(name);

            if (encoding is null)
            {
                return span.GetAsciiOrUTF8StringNonNullCharacters(DefaultRequestHeaderEncoding);
            }

            if (ReferenceEquals(encoding, Encoding.Latin1))
            {
                return span.GetLatin1StringNonNullCharacters();
            }

            try
            {
                return encoding.GetString(span);
            }
            catch (DecoderFallbackException)
            {
                throw new InvalidOperationException();
            }
        }

        public static string GetAsciiStringEscaped(this ReadOnlySpan<byte> span, int maxChars)
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
        public static bool GetKnownMethod(this ReadOnlySpan<byte> span, out HttpMethod method, out int length)
        {
            method = GetKnownMethod(span, out length);
            return method != HttpMethod.Custom;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HttpMethod GetKnownMethod(this ReadOnlySpan<byte> span, out int methodLength)
        {
            methodLength = 0;
            if (sizeof(uint) <= span.Length)
            {
                if (BinaryPrimitives.ReadUInt32LittleEndian(span) == _httpGetMethodInt)
                {
                    methodLength = 3;
                    return HttpMethod.Get;
                }
                else if (sizeof(ulong) <= span.Length)
                {
                    var value = BinaryPrimitives.ReadUInt64LittleEndian(span);
                    var index = GetKnownMethodIndex(value);
                    var knownMehods = _knownMethods;
                    if ((uint)index < (uint)knownMehods.Length)
                    {
                        var knownMethod = _knownMethods[index];

                        if (knownMethod != null && (value & knownMethod.Item1) == knownMethod.Item2)
                        {
                            methodLength = knownMethod.Item4;
                            return knownMethod.Item3;
                        }
                    }
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
                return HttpMethod.None;
            }

            var length = value.Length;
            if (length == 0)
            {
                return HttpMethod.None;
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
                else if (firstChar == 'P' && string.Equals(value, HttpMethods.Post, StringComparison.Ordinal))
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
                else if (firstChar == 'P' && string.Equals(value, HttpMethods.Patch, StringComparison.Ordinal))
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
        public static bool GetKnownVersion(this ReadOnlySpan<byte> span, out HttpVersion knownVersion, out byte length)
        {
            knownVersion = GetKnownVersionAndConfirmCR(span);
            if (knownVersion != HttpVersion.Unknown)
            {
                length = sizeof(ulong);
                return true;
            }

            length = 0;
            return false;
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
        internal static HttpVersion GetKnownVersionAndConfirmCR(this ReadOnlySpan<byte> location)
        {
            if (location.Length < sizeof(ulong))
            {
                return HttpVersion.Unknown;
            }
            else
            {
                var version = BinaryPrimitives.ReadUInt64LittleEndian(location);
                if (sizeof(ulong) >= (uint)location.Length || location[sizeof(ulong)] != (byte)'\r')
                {
                    return HttpVersion.Unknown;
                }
                else if (version == _http11VersionLong)
                {
                    return HttpVersion.Http11;
                }
                else if (version == _http10VersionLong)
                {
                    return HttpVersion.Http10;
                }
            }

            return HttpVersion.Unknown;
        }

        /// <summary>
        /// Checks 8 bytes from <paramref name="span"/> that correspond to 'http://' or 'https://'
        /// </summary>
        /// <param name="span">The span</param>
        /// <param name="knownScheme">A reference to the known scheme, if the input matches any</param>
        /// <returns>True when memory starts with known http or https schema</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetKnownHttpScheme(this Span<byte> span, out HttpScheme knownScheme)
        {
            if (BinaryPrimitives.TryReadUInt64LittleEndian(span, out var scheme))
            {
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
                    return AspNetCore.Http.HttpProtocol.Http10;
                case HttpVersion.Http11:
                    return AspNetCore.Http.HttpProtocol.Http11;
                case HttpVersion.Http2:
                    return AspNetCore.Http.HttpProtocol.Http2;
                case HttpVersion.Http3:
                    return AspNetCore.Http.HttpProtocol.Http3;
                default:
                    Debug.Fail("Unexpected HttpVersion: " + httpVersion);
                    return null;
            };
        }

        public static string MethodToString(HttpMethod method)
        {
            var methodIndex = (int)method;
            var methodNames = _methodNames;
            if ((uint)methodIndex < (uint)methodNames.Length)
            {
                return methodNames[methodIndex];
            }
            return null;
        }

        public static string SchemeToString(HttpScheme scheme)
        {
            return scheme switch
            {
                HttpScheme.Http => HttpUriScheme,
                HttpScheme.Https => HttpsUriScheme,
                _ => null,
            };
        }

        public static bool IsHostHeaderValid(string hostText)
        {
            if (string.IsNullOrEmpty(hostText))
            {
                // The spec allows empty values
                return true;
            }

            var firstChar = hostText[0];
            if (firstChar == '[')
            {
                // Tail call
                return IsIPv6HostValid(hostText);
            }
            else
            {
                if (firstChar == ':')
                {
                    // Only a port
                    return false;
                }

                var invalid = HttpCharacters.IndexOfInvalidHostChar(hostText);
                if (invalid >= 0)
                {
                    // Tail call
                    return IsHostPortValid(hostText, invalid);
                }

                return true;
            }
        }

        // The lead '[' was already checked
        private static bool IsIPv6HostValid(string hostText)
        {
            for (var i = 1; i < hostText.Length; i++)
            {
                var ch = hostText[i];
                if (ch == ']')
                {
                    // [::1] is the shortest valid IPv6 host
                    if (i < 4)
                    {
                        return false;
                    }
                    else if (i + 1 < hostText.Length)
                    {
                        // Tail call
                        return IsHostPortValid(hostText, i + 1);
                    }
                    return true;
                }

                if (!IsHex(ch) && ch != ':' && ch != '.')
                {
                    return false;
                }
            }

            // Must contain a ']'
            return false;
        }

        private static bool IsHostPortValid(string hostText, int offset)
        {
            var firstChar = hostText[offset];
            offset++;
            if (firstChar != ':' || offset == hostText.Length)
            {
                // Must have at least one number after the colon if present.
                return false;
            }

            for (var i = offset; i < hostText.Length; i++)
            {
                if (!IsNumeric(hostText[i]))
                {
                    return false;
                }
            }

            return true;
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

        // Allow for de-virtualization (see https://github.com/dotnet/coreclr/pull/9230)	
        private sealed class UTF8EncodingSealed : UTF8Encoding	
        {	
            public UTF8EncodingSealed() : base(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true) { }	

            public override byte[] GetPreamble() => Array.Empty<byte>();	
        }
    }
}
