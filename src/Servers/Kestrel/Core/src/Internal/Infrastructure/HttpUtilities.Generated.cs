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
        private static readonly ulong s_httpConnectMethodLong = GetAsciiStringAsLong("CONNECT ");
        private static readonly ulong s_httpDeleteMethodLong = GetAsciiStringAsLong("DELETE \0");
        private static readonly ulong s_httpHeadMethodLong = GetAsciiStringAsLong("HEAD \0\0\0");
        private static readonly ulong s_httpPatchMethodLong = GetAsciiStringAsLong("PATCH \0\0");
        private static readonly ulong s_httpPostMethodLong = GetAsciiStringAsLong("POST \0\0\0");
        private static readonly ulong s_httpPutMethodLong = GetAsciiStringAsLong("PUT \0\0\0\0");
        private static readonly ulong s_httpOptionsMethodLong = GetAsciiStringAsLong("OPTIONS ");
        private static readonly ulong s_httpTraceMethodLong = GetAsciiStringAsLong("TRACE \0\0");

        private static readonly ulong s_mask8Chars = GetMaskAsLong([0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]);

        private static readonly ulong s_mask7Chars = GetMaskAsLong([0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00]);

        private static readonly ulong s_mask6Chars = GetMaskAsLong([0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00]);

        private static readonly ulong s_mask5Chars = GetMaskAsLong([0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00]);

        private static readonly ulong s_mask4Chars = GetMaskAsLong([0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00]);

        private static readonly Tuple<ulong, ulong, HttpMethod, int>[] s_knownMethods =
            new Tuple<ulong, ulong, HttpMethod, int>[17];

        private static readonly string[] s_methodNames = new string[9];

        static HttpUtilities()
        {
            SetKnownMethod(s_mask4Chars, s_httpPutMethodLong, HttpMethod.Put, 3);
            SetKnownMethod(s_mask5Chars, s_httpHeadMethodLong, HttpMethod.Head, 4);
            SetKnownMethod(s_mask5Chars, s_httpPostMethodLong, HttpMethod.Post, 4);
            SetKnownMethod(s_mask6Chars, s_httpPatchMethodLong, HttpMethod.Patch, 5);
            SetKnownMethod(s_mask6Chars, s_httpTraceMethodLong, HttpMethod.Trace, 5);
            SetKnownMethod(s_mask7Chars, s_httpDeleteMethodLong, HttpMethod.Delete, 6);
            SetKnownMethod(s_mask8Chars, s_httpConnectMethodLong, HttpMethod.Connect, 7);
            SetKnownMethod(s_mask8Chars, s_httpOptionsMethodLong, HttpMethod.Options, 7);
            FillKnownMethodsGaps();
            s_methodNames[(byte)HttpMethod.Connect] = HttpMethods.Connect;
            s_methodNames[(byte)HttpMethod.Delete] = HttpMethods.Delete;
            s_methodNames[(byte)HttpMethod.Get] = HttpMethods.Get;
            s_methodNames[(byte)HttpMethod.Head] = HttpMethods.Head;
            s_methodNames[(byte)HttpMethod.Options] = HttpMethods.Options;
            s_methodNames[(byte)HttpMethod.Patch] = HttpMethods.Patch;
            s_methodNames[(byte)HttpMethod.Post] = HttpMethods.Post;
            s_methodNames[(byte)HttpMethod.Put] = HttpMethods.Put;
            s_methodNames[(byte)HttpMethod.Trace] = HttpMethods.Trace;
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
