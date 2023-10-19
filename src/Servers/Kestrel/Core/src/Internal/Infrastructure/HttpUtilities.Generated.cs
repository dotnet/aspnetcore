// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

#nullable enable

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal static partial class HttpUtilities
    {
        // readonly primitive statics can be Jit'd to consts https://github.com/dotnet/coreclr/issues/1079
        private static readonly ulong _httpConnectMethodLong = GetAsciiStringAsLong("CONNECT ");
        private static readonly ulong _httpDeleteMethodLong = GetAsciiStringAsLong("DELETE \0");
        private static readonly ulong _httpHeadMethodLong = GetAsciiStringAsLong("HEAD \0\0\0");
        private static readonly ulong _httpPatchMethodLong = GetAsciiStringAsLong("PATCH \0\0");
        private static readonly ulong _httpPostMethodLong = GetAsciiStringAsLong("POST \0\0\0");
        private static readonly ulong _httpPutMethodLong = GetAsciiStringAsLong("PUT \0\0\0\0");
        private static readonly ulong _httpOptionsMethodLong = GetAsciiStringAsLong("OPTIONS ");
        private static readonly ulong _httpTraceMethodLong = GetAsciiStringAsLong("TRACE \0\0");

        private static readonly ulong _mask8Chars = GetMaskAsLong([0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]);

        private static readonly ulong _mask7Chars = GetMaskAsLong([0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00]);

        private static readonly ulong _mask6Chars = GetMaskAsLong([0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00]);

        private static readonly ulong _mask5Chars = GetMaskAsLong([0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00]);

        private static readonly ulong _mask4Chars = GetMaskAsLong([0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00]);

        private static readonly Tuple<ulong, ulong, HttpMethod, int>[] _knownMethods =
            new Tuple<ulong, ulong, HttpMethod, int>[17];

        private static readonly string[] _methodNames = new string[9];

        static HttpUtilities()
        {
            SetKnownMethod(_mask4Chars, _httpPutMethodLong, HttpMethod.Put, 3);
            SetKnownMethod(_mask5Chars, _httpHeadMethodLong, HttpMethod.Head, 4);
            SetKnownMethod(_mask5Chars, _httpPostMethodLong, HttpMethod.Post, 4);
            SetKnownMethod(_mask6Chars, _httpPatchMethodLong, HttpMethod.Patch, 5);
            SetKnownMethod(_mask6Chars, _httpTraceMethodLong, HttpMethod.Trace, 5);
            SetKnownMethod(_mask7Chars, _httpDeleteMethodLong, HttpMethod.Delete, 6);
            SetKnownMethod(_mask8Chars, _httpConnectMethodLong, HttpMethod.Connect, 7);
            SetKnownMethod(_mask8Chars, _httpOptionsMethodLong, HttpMethod.Options, 7);
            FillKnownMethodsGaps();
            _methodNames[(byte)HttpMethod.Connect] = HttpMethods.Connect;
            _methodNames[(byte)HttpMethod.Delete] = HttpMethods.Delete;
            _methodNames[(byte)HttpMethod.Get] = HttpMethods.Get;
            _methodNames[(byte)HttpMethod.Head] = HttpMethods.Head;
            _methodNames[(byte)HttpMethod.Options] = HttpMethods.Options;
            _methodNames[(byte)HttpMethod.Patch] = HttpMethods.Patch;
            _methodNames[(byte)HttpMethod.Post] = HttpMethods.Post;
            _methodNames[(byte)HttpMethod.Put] = HttpMethods.Put;
            _methodNames[(byte)HttpMethod.Trace] = HttpMethods.Trace;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetKnownMethodIndex(ulong value)
        {
            const int magicNumer = 0x600000C;
            var tmp = (int)value & magicNumer;
            return ((tmp >> 2) | (tmp >> 23)) & 0xF;
        }
    }
}