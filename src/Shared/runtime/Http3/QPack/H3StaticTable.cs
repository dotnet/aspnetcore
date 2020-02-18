// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;

namespace System.Net.Http.QPack
{
    // TODO: make class static.
    internal class H3StaticTable
    {
        private readonly Dictionary<int, int> _statusIndex = new Dictionary<int, int>
        {
            [103] = 24,
            [200] = 25,
            [304] = 26,
            [404] = 27,
            [503] = 28,
            [100] = 63,
            [204] = 64,
            [206] = 65,
            [302] = 66,
            [400] = 67,
            [403] = 68,
            [421] = 69,
            [425] = 70,
            [500] = 71,
        };

        private readonly Dictionary<HttpMethod, int> _methodIndex = new Dictionary<HttpMethod, int>
        {
            // TODO connect is internal to system.net.http
            [HttpMethod.Delete] = 16,
            [HttpMethod.Get] = 17,
            [HttpMethod.Head] = 18,
            [HttpMethod.Options] = 19,
            [HttpMethod.Post] = 20,
            [HttpMethod.Put] = 21,
        };

        private H3StaticTable()
        {
        }

        public static H3StaticTable Instance { get; } = new H3StaticTable();

        public int Count => _staticTable.Length;

        public HeaderField this[int index] => _staticTable[index];

        // TODO: just use Dictionary directly to avoid interface dispatch.
        public IReadOnlyDictionary<int, int> StatusIndex => _statusIndex;
        public IReadOnlyDictionary<HttpMethod, int> MethodIndex => _methodIndex;

        private readonly HeaderField[] _staticTable = new HeaderField[]
        {
            CreateHeaderField(":authority", ""), // 0
            CreateHeaderField(":path", "/"), // 1
            CreateHeaderField("age", "0"), // 2
            CreateHeaderField("content-disposition", ""),
            CreateHeaderField("content-length", "0"),
            CreateHeaderField("cookie", ""),
            CreateHeaderField("date", ""),
            CreateHeaderField("etag", ""),
            CreateHeaderField("if-modified-since", ""),
            CreateHeaderField("if-none-match", ""),
            CreateHeaderField("last-modified", ""), // 10
            CreateHeaderField("link", ""),
            CreateHeaderField("location", ""),
            CreateHeaderField("referer", ""),
            CreateHeaderField("set-cookie", ""),
            CreateHeaderField(":method", "CONNECT"),
            CreateHeaderField(":method", "DELETE"),
            CreateHeaderField(":method", "GET"),
            CreateHeaderField(":method", "HEAD"),
            CreateHeaderField(":method", "OPTIONS"),
            CreateHeaderField(":method", "POST"), // 20
            CreateHeaderField(":method", "PUT"),
            CreateHeaderField(":scheme", "http"),
            CreateHeaderField(":scheme", "https"),
            CreateHeaderField(":status", "103"),
            CreateHeaderField(":status", "200"),
            CreateHeaderField(":status", "304"),
            CreateHeaderField(":status", "404"),
            CreateHeaderField(":status", "503"),
            CreateHeaderField("accept", "*/*"),
            CreateHeaderField("accept", "application/dns-message"), // 30
            CreateHeaderField("accept-encoding", "gzip, deflate, br"),
            CreateHeaderField("accept-ranges", "bytes"),
            CreateHeaderField("access-control-allow-headers", "cache-control"),
            CreateHeaderField("access-control-allow-origin", "content-type"),
            CreateHeaderField("access-control-allow-origin", "*"),
            CreateHeaderField("cache-control", "max-age=0"),
            CreateHeaderField("cache-control", "max-age=2592000"),
            CreateHeaderField("cache-control", "max-age=604800"),
            CreateHeaderField("cache-control", "no-cache"),
            CreateHeaderField("cache-control", "no-store"), // 40
            CreateHeaderField("cache-control", "public, max-age=31536000"),
            CreateHeaderField("content-encoding", "br"),
            CreateHeaderField("content-encoding", "gzip"),
            CreateHeaderField("content-type", "application/dns-message"),
            CreateHeaderField("content-type", "application/javascript"),
            CreateHeaderField("content-type", "application/json"),
            CreateHeaderField("content-type", "application/x-www-form-urlencoded"),
            CreateHeaderField("content-type", "image/gif"),
            CreateHeaderField("content-type", "image/jpeg"),
            CreateHeaderField("content-type", "image/png"), // 50
            CreateHeaderField("content-type", "text/css"),
            CreateHeaderField("content-type", "text/html; charset=utf-8"),
            CreateHeaderField("content-type", "text/plain"),
            CreateHeaderField("content-type", "text/plain;charset=utf-8"),
            CreateHeaderField("range", "bytes=0-"),
            CreateHeaderField("strict-transport-security", "max-age=31536000"),
            CreateHeaderField("strict-transport-security", "max-age=31536000;includesubdomains"), // TODO confirm spaces here don't matter?
            CreateHeaderField("strict-transport-security", "max-age=31536000;includesubdomains; preload"),
            CreateHeaderField("vary", "accept-encoding"),
            CreateHeaderField("vary", "origin"), // 60
            CreateHeaderField("x-content-type-options", "nosniff"),
            CreateHeaderField("x-xss-protection", "1; mode=block"),
            CreateHeaderField(":status", "100"),
            CreateHeaderField(":status", "204"),
            CreateHeaderField(":status", "206"),
            CreateHeaderField(":status", "302"),
            CreateHeaderField(":status", "400"),
            CreateHeaderField(":status", "403"),
            CreateHeaderField(":status", "421"),
            CreateHeaderField(":status", "425"), // 70
            CreateHeaderField(":status", "500"),
            CreateHeaderField("accept-language", ""),
            CreateHeaderField("access-control-allow-credentials", "FALSE"),
            CreateHeaderField("access-control-allow-credentials", "TRUE"),
            CreateHeaderField("access-control-allow-headers", "*"),
            CreateHeaderField("access-control-allow-methods", "get"),
            CreateHeaderField("access-control-allow-methods", "get, post, options"),
            CreateHeaderField("access-control-allow-methods", "options"),
            CreateHeaderField("access-control-expose-headers", "content-length"),
            CreateHeaderField("access-control-request-headers", "content-type"), // 80
            CreateHeaderField("access-control-request-method", "get"),
            CreateHeaderField("access-control-request-method", "post"),
            CreateHeaderField("alt-svc", "clear"),
            CreateHeaderField("authorization", ""),
            CreateHeaderField("content-security-policy", "script-src 'none'; object-src 'none'; base-uri 'none'"),
            CreateHeaderField("early-data", "1"),
            CreateHeaderField("expect-ct", ""),
            CreateHeaderField("forwarded", ""),
            CreateHeaderField("if-range", ""),
            CreateHeaderField("origin", ""), // 90
            CreateHeaderField("purpose", "prefetch"),
            CreateHeaderField("server", ""),
            CreateHeaderField("timing-allow-origin", "*"),
            CreateHeaderField("upgrading-insecure-requests", "1"),
            CreateHeaderField("user-agent", ""),
            CreateHeaderField("x-forwarded-for", ""),
            CreateHeaderField("x-frame-options", "deny"),
            CreateHeaderField("x-frame-options", "sameorigin"),
        };

        private static HeaderField CreateHeaderField(string name, string value)
            => new HeaderField(Encoding.ASCII.GetBytes(name), Encoding.ASCII.GetBytes(value));

        public const int Authority = 0;
        public const int PathSlash = 1;
        public const int Age0 = 2;
        public const int ContentDisposition = 3;
        public const int ContentLength0 = 4;
        public const int Cookie = 5;
        public const int Date = 6;
        public const int ETag = 7;
        public const int IfModifiedSince = 8;
        public const int IfNoneMatch = 9;
        public const int LastModified = 10;
        public const int Link = 11;
        public const int Location = 12;
        public const int Referer = 13;
        public const int SetCookie = 14;
        public const int MethodConnect = 15;
        public const int MethodDelete = 16;
        public const int MethodGet = 17;
        public const int MethodHead = 18;
        public const int MethodOptions = 19;
        public const int MethodPost = 20;
        public const int MethodPut = 21;
        public const int SchemeHttps = 23;
        public const int Status103 = 24;
        public const int Status200 = 25;
        public const int Status304 = 26;
        public const int Status404 = 27;
        public const int Status503 = 28;
        public const int AcceptAny = 29;
        public const int AcceptEncodingGzipDeflateBr = 31;
        public const int AcceptRangesBytes = 32;
        public const int AccessControlAllowHeadersCacheControl = 33;
        public const int AccessControlAllowOriginAny = 35;
        public const int CacheControlMaxAge0 = 36;
        public const int ContentEncodingBr = 42;
        public const int ContentTypeApplicationDnsMessage = 44;
        public const int RangeBytes0ToAll = 55;
        public const int StrictTransportSecurityMaxAge31536000 = 56;
        public const int VaryAcceptEncoding = 59;
        public const int XContentTypeOptionsNoSniff = 61;
        public const int Status100 = 63;
        public const int Status204 = 64;
        public const int Status206 = 65;
        public const int Status302 = 66;
        public const int Status400 = 67;
        public const int Status403 = 68;
        public const int Status421 = 69;
        public const int Status425 = 70;
        public const int Status500 = 71;
        public const int AcceptLanguage = 72;
        public const int AccessControlAllowCredentials = 73;
        public const int AccessControlAllowMethodsGet = 76;
        public const int AccessControlExposeHeadersContentLength = 79;
        public const int AltSvcClear = 83;
        public const int Authorization = 84;
        public const int ContentSecurityPolicyAllNone = 85;
        public const int IfRange = 89;
        public const int Origin = 90;
        public const int Server = 92;
        public const int UpgradeInsecureRequests1 = 94;
        public const int UserAgent = 95;
        public const int XFrameOptionsDeny = 97;
    }
}
