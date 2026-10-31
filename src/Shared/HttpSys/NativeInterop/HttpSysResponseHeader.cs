// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal enum HttpSysResponseHeader
{
    CacheControl = 0,    // general-header [section 4.5]
    Connection = 1,    // general-header [section 4.5]
    Date = 2,    // general-header [section 4.5]
    KeepAlive = 3,    // general-header [not in rfc]
    Pragma = 4,    // general-header [section 4.5]
    Trailer = 5,    // general-header [section 4.5]
    TransferEncoding = 6,    // general-header [section 4.5]
    Upgrade = 7,    // general-header [section 4.5]
    Via = 8,    // general-header [section 4.5]
    Warning = 9,    // general-header [section 4.5]
    Allow = 10,   // entity-header  [section 7.1]
    ContentLength = 11,   // entity-header  [section 7.1]
    ContentType = 12,   // entity-header  [section 7.1]
    ContentEncoding = 13,   // entity-header  [section 7.1]
    ContentLanguage = 14,   // entity-header  [section 7.1]
    ContentLocation = 15,   // entity-header  [section 7.1]
    ContentMd5 = 16,   // entity-header  [section 7.1]
    ContentRange = 17,   // entity-header  [section 7.1]
    Expires = 18,   // entity-header  [section 7.1]
    LastModified = 19,   // entity-header  [section 7.1]

    AcceptRanges = 20,   // response-header [section 6.2]
    Age = 21,   // response-header [section 6.2]
    ETag = 22,   // response-header [section 6.2]
    Location = 23,   // response-header [section 6.2]
    ProxyAuthenticate = 24,   // response-header [section 6.2]
    RetryAfter = 25,   // response-header [section 6.2]
    Server = 26,   // response-header [section 6.2]
    SetCookie = 27,   // response-header [not in rfc]
    Vary = 28,   // response-header [section 6.2]
    WwwAuthenticate = 29,   // response-header [section 6.2]
}
