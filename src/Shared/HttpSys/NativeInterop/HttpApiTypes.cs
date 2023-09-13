
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Runtime.InteropServices;
using Microsoft.Net.Http.Headers;
using Windows.Win32;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal static unsafe class HttpApiTypes
{
    internal enum HTTP_REQUEST_PROPERTY
    {
        HttpRequestPropertyIsb,
        HttpRequestPropertyTcpInfoV0,
        HttpRequestPropertyQuicStats,
        HttpRequestPropertyTcpInfoV1,
        HttpRequestPropertySni,
        HttpRequestPropertyStreamError,
    }

    // 255 + the null terminator
    internal const int SniPropertySizeInBytes = (int)((sizeof(ushort) * (PInvoke.HTTP_REQUEST_PROPERTY_SNI_HOST_MAX_LENGTH + 1)) + sizeof(uint));

    // HTTP_PROPERTY_FLAGS.Present
    internal const uint HTTP_PROPERTY_FLAGS_PRESENT = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    internal struct HTTPAPI_VERSION
    {
        internal ushort HttpApiMajorVersion;
        internal ushort HttpApiMinorVersion;

        public static explicit operator HTTPAPI_VERSION(Windows.Win32.Networking.HttpServer.HTTPAPI_VERSION v) => new()
        {
            HttpApiMajorVersion = v.HttpApiMajorVersion,
            HttpApiMinorVersion = v.HttpApiMinorVersion
        };
    }

    internal static class ResponseHeaders
    {
        private static readonly string[] _strings =
        {
            HeaderNames.CacheControl,
            HeaderNames.Connection,
            HeaderNames.Date,
            HeaderNames.KeepAlive,
            HeaderNames.Pragma,
            HeaderNames.Trailer,
            HeaderNames.TransferEncoding,
            HeaderNames.Upgrade,
            HeaderNames.Via,
            HeaderNames.Warning,

            HeaderNames.Allow,
            HeaderNames.ContentLength,
            HeaderNames.ContentType,
            HeaderNames.ContentEncoding,
            HeaderNames.ContentLanguage,
            HeaderNames.ContentLocation,
            HeaderNames.ContentMD5,
            HeaderNames.ContentRange,
            HeaderNames.Expires,
            HeaderNames.LastModified,

            HeaderNames.AcceptRanges,
            HeaderNames.Age,
            HeaderNames.ETag,
            HeaderNames.Location,
            HeaderNames.ProxyAuthenticate,
            HeaderNames.RetryAfter,
            HeaderNames.Server,
            HeaderNames.SetCookie,
            HeaderNames.Vary,
            HeaderNames.WWWAuthenticate,
        };

        internal static FrozenDictionary<string, int> KnownHeaders { get; } = CreateLookupTable();

        private static FrozenDictionary<string, int> CreateLookupTable()
        {
            var lookupTable = new Dictionary<string, int>((int)HTTP_HEADER_ID.HttpHeaderResponseMaximum, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < (int)HTTP_HEADER_ID.HttpHeaderResponseMaximum; i++)
            {
                lookupTable.Add(_strings[i], i);
            }
            return lookupTable.ToFrozenDictionary();
        }
    }
}
