// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpSys.Internal;

// With the byte a ROS<HttpSysRequestHeader> can be created that referes to assembly's static data segment.
internal enum HttpSysRequestHeader : byte
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

    Accept = 20,   // request-header [section 5.3]
    AcceptCharset = 21,   // request-header [section 5.3]
    AcceptEncoding = 22,   // request-header [section 5.3]
    AcceptLanguage = 23,   // request-header [section 5.3]
    Authorization = 24,   // request-header [section 5.3]
    Cookie = 25,   // request-header [not in rfc]
    Expect = 26,   // request-header [section 5.3]
    From = 27,   // request-header [section 5.3]
    Host = 28,   // request-header [section 5.3]
    IfMatch = 29,   // request-header [section 5.3]
    IfModifiedSince = 30,   // request-header [section 5.3]
    IfNoneMatch = 31,   // request-header [section 5.3]
    IfRange = 32,   // request-header [section 5.3]
    IfUnmodifiedSince = 33,   // request-header [section 5.3]
    MaxForwards = 34,   // request-header [section 5.3]
    ProxyAuthorization = 35,   // request-header [section 5.3]
    Referer = 36,   // request-header [section 5.3]
    Range = 37,   // request-header [section 5.3]
    Te = 38,   // request-header [section 5.3]
    Translate = 39,   // request-header [webDAV, not in rfc 2518]
    UserAgent = 40,   // request-header [section 5.3]
}
