// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal static class HttpCharacters
    {
        private static readonly int _tableSize = 128;
        private static readonly bool[] _alphaNumeric = InitializeAlphaNumeric();
        private static readonly bool[] _authority = InitializeAuthority();
        private static readonly bool[] _token = InitializeToken();
        private static readonly bool[] _host = InitializeHost();
        private static readonly bool[] _fieldValue = InitializeFieldValue();

        internal static void Initialize()
        {
            // Access _alphaNumeric to initialize static fields
            var initialize = _alphaNumeric;
        }

        private static bool[] InitializeAlphaNumeric()
        {
            // ALPHA and DIGIT https://tools.ietf.org/html/rfc5234#appendix-B.1
            var alphaNumeric = new bool[_tableSize];
            for (var c = '0'; c <= '9'; c++)
            {
                alphaNumeric[c] = true;
            }
            for (var c = 'A'; c <= 'Z'; c++)
            {
                alphaNumeric[c] = true;
            }
            for (var c = 'a'; c <= 'z'; c++)
            {
                alphaNumeric[c] = true;
            }
            return alphaNumeric;
        }

        private static bool[] InitializeAuthority()
        {
            // Authority https://tools.ietf.org/html/rfc3986#section-3.2
            // Examples:
            // microsoft.com
            // hostname:8080
            // [::]:8080
            // [fe80::]
            // 127.0.0.1
            // user@host.com
            // user:password@host.com
            var authority = new bool[_tableSize];
            Array.Copy(_alphaNumeric, authority, _tableSize);
            authority[':'] = true;
            authority['.'] = true;
            authority['['] = true;
            authority[']'] = true;
            authority['@'] = true;
            return authority;
        }

        private static bool[] InitializeToken()
        {
            // tchar https://tools.ietf.org/html/rfc7230#appendix-B
            var token = new bool[_tableSize];
            Array.Copy(_alphaNumeric, token, _tableSize);
            token['!'] = true;
            token['#'] = true;
            token['$'] = true;
            token['%'] = true;
            token['&'] = true;
            token['\''] = true;
            token['*'] = true;
            token['+'] = true;
            token['-'] = true;
            token['.'] = true;
            token['^'] = true;
            token['_'] = true;
            token['`'] = true;
            token['|'] = true;
            token['~'] = true;
            return token;
        }

        private static bool[] InitializeHost()
        {
            // Matches Http.Sys
            // Matches RFC 3986 except "*" / "+" / "," / ";" / "=" and "%" HEXDIG HEXDIG which are not allowed by Http.Sys
            var host = new bool[_tableSize];
            Array.Copy(_alphaNumeric, host, _tableSize);
            host['!'] = true;
            host['$'] = true;
            host['&'] = true;
            host['\''] = true;
            host['('] = true;
            host[')'] = true;
            host['-'] = true;
            host['.'] = true;
            host['_'] = true;
            host['~'] = true;
            return host;
        }

        private static bool[] InitializeFieldValue()
        {
            // field-value https://tools.ietf.org/html/rfc7230#section-3.2
            var fieldValue = new bool[_tableSize];
            for (var c = 0x20; c <= 0x7e; c++) // VCHAR and SP
            {
                fieldValue[c] = true;
            }
            return fieldValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsInvalidAuthorityChar(Span<byte> s)
        {
            var authority = _authority;

            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c >= (uint)authority.Length || !authority[c])
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfInvalidHostChar(string s)
        {
            var host = _host;

            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c >= (uint)host.Length || !host[c])
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfInvalidTokenChar(string s)
        {
            var token = _token;

            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c >= (uint)token.Length || !token[c])
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int IndexOfInvalidTokenChar(byte* s, int length)
        {
            var token = _token;

            for (var i = 0; i < length; i++)
            {
                var c = s[i];
                if (c >= (uint)token.Length || !token[c])
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfInvalidFieldValueChar(string s)
        {
            var fieldValue = _fieldValue;

            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c >= (uint)fieldValue.Length || !fieldValue[c])
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
