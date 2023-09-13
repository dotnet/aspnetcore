
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using Microsoft.Net.Http.Headers;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.HttpSys.Internal;
#pragma warning disable IDE0044 // Add readonly modifier. We don't want to modify these interop types

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

    private const int HTTP_REQUEST_PROPERTY_SNI_HOST_MAX_LENGTH = 255;
    internal const int SniPropertySizeInBytes = (sizeof(ushort) * (HTTP_REQUEST_PROPERTY_SNI_HOST_MAX_LENGTH + 1)) + sizeof(uint);

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

    internal enum HTTP_RESPONSE_INFO_FLAGS : uint
    {
        None = 0,
        PreserveOrder = 1,
    }

    [Flags]
    internal enum HTTP_REQUEST_FLAGS
    {
        None = 0,
        MoreEntityBodyExists = 1,
        IPRouted = 2,
        Http2 = 4,
        Http3 = 8,
    }

    // see http.w for definitions
    [Flags]
    internal enum HTTP_FLAGS : uint
    {
        NONE = 0x00000000,
        HTTP_SEND_RESPONSE_FLAG_DISCONNECT = 0x00000001,
        HTTP_SEND_RESPONSE_FLAG_MORE_DATA = 0x00000002,
        HTTP_SEND_RESPONSE_FLAG_BUFFER_DATA = 0x00000004,
        HTTP_SEND_RESPONSE_FLAG_RAW_HEADER = 0x00000004,
        HTTP_SEND_REQUEST_FLAG_MORE_DATA = 0x00000001,
        HTTP_PROPERTY_FLAG_PRESENT = 0x00000001,
        HTTP_INITIALIZE_CBT = 0x00000004,
        HTTP_SEND_RESPONSE_FLAG_OPAQUE = 0x00000040,
        HTTP_SEND_RESPONSE_FLAG_GOAWAY = 0x00000100,
    }

    [Flags]
    internal enum HTTP_AUTH_TYPES : uint
    {
        NONE = 0x00000000,
        HTTP_AUTH_ENABLE_BASIC = 0x00000001,
        HTTP_AUTH_ENABLE_DIGEST = 0x00000002,
        HTTP_AUTH_ENABLE_NTLM = 0x00000004,
        HTTP_AUTH_ENABLE_NEGOTIATE = 0x00000008,
        HTTP_AUTH_ENABLE_KERBEROS = 0x00000010,
    }

    [Flags]
    internal enum HTTP_CREATE_REQUEST_QUEUE_FLAG : uint
    {
        None = 0,
        // The HTTP_CREATE_REQUEST_QUEUE_FLAG_OPEN_EXISTING flag allows applications to open an existing request queue by name and retrieve the request queue handle. The pName parameter must contain a valid request queue name; it cannot be NULL.
        OpenExisting = 1,
        // The handle to the request queue created using this flag cannot be used to perform I/O operations. This flag can be set only when the request queue handle is created.
        Controller = 2,
        Delegation = 8
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
