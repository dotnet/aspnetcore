
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using Microsoft.Net.Http.Headers;
using Windows.Win32;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal static unsafe class HttpApiTypes
{
    // 255 + the null terminator
    internal const int SniPropertySizeInBytes = (int)((sizeof(ushort) * (PInvoke.HTTP_REQUEST_PROPERTY_SNI_HOST_MAX_LENGTH + 1)) + sizeof(uint));

    internal static FrozenDictionary<string, int> KnownResponseHeaders { get; } = CreateLookupTable();

    private static FrozenDictionary<string, int> CreateLookupTable()
    {
        // See https://learn.microsoft.com/windows/win32/api/http/ne-http-http_header_id
        string[] headerNames =
        [
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
        ];

        var index = 0;
        return headerNames.ToFrozenDictionary(s => s, _ => index++, StringComparer.OrdinalIgnoreCase);
    }
}
