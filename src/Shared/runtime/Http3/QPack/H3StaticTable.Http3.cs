// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace System.Net.Http.QPack
{
    internal static partial class H3StaticTable
    {
        private static readonly Dictionary<int, int> s_statusIndex = new Dictionary<int, int>
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

        private static readonly Dictionary<HttpMethod, int> s_methodIndex = new Dictionary<HttpMethod, int>
        {
            // TODO connect is internal to system.net.http
            [HttpMethod.Delete] = 16,
            [HttpMethod.Get] = 17,
            [HttpMethod.Head] = 18,
            [HttpMethod.Options] = 19,
            [HttpMethod.Post] = 20,
            [HttpMethod.Put] = 21,
        };

        public static int Count => s_staticTable.Length;

        // TODO: just use Dictionary directly to avoid interface dispatch.
        public static IReadOnlyDictionary<int, int> StatusIndex => s_statusIndex;
        public static IReadOnlyDictionary<HttpMethod, int> MethodIndex => s_methodIndex;

        public static HeaderField GetHeaderFieldAt(int index) => s_staticTable[index];

        private static readonly HeaderField[] s_staticTable = new HeaderField[]
        {
            CreateHeaderField(":authority", ""), // 0
            CreateHeaderField(":path", "/"), // 1
            CreateHeaderField("age", "0"), // 2
            CreateHeaderField("content-disposition", ""), //3
            CreateHeaderField("content-length", "0"), // 4
            CreateHeaderField("cookie", ""), // 5
            CreateHeaderField("date", ""), // 6
            CreateHeaderField("etag", ""), // 7
            CreateHeaderField("if-modified-since", ""), // 8
            CreateHeaderField("if-none-match", ""), // 9
            CreateHeaderField("last-modified", ""), // 10
            CreateHeaderField("link", ""), // 11
            CreateHeaderField("location", ""), // 12
            CreateHeaderField("referer", ""), // 13
            CreateHeaderField("set-cookie", ""), // 14
            CreateHeaderField(":method", "CONNECT"), // 15
            CreateHeaderField(":method", "DELETE"), // 16
            CreateHeaderField(":method", "GET"), // 17
            CreateHeaderField(":method", "HEAD"), // 18
            CreateHeaderField(":method", "OPTIONS"), // 19
            CreateHeaderField(":method", "POST"), // 20
            CreateHeaderField(":method", "PUT"), // 21
            CreateHeaderField(":scheme", "http"), // 22
            CreateHeaderField(":scheme", "https"), // 23
            CreateHeaderField(":status", "103"), // 24
            CreateHeaderField(":status", "200"), // 25
            CreateHeaderField(":status", "304"), // 26
            CreateHeaderField(":status", "404"), // 27
            CreateHeaderField(":status", "503"), // 28
            CreateHeaderField("accept", "*/*"), //29
            CreateHeaderField("accept", "application/dns-message"), // 30
            CreateHeaderField("accept-encoding", "gzip, deflate, br"), // 31
            CreateHeaderField("accept-ranges", "bytes"), // 32
            CreateHeaderField("access-control-allow-headers", "cache-control"), // 33
            CreateHeaderField("access-control-allow-headers", "content-type"), // 34
            CreateHeaderField("access-control-allow-origin", "*"), // 35
            CreateHeaderField("cache-control", "max-age=0"), // 36
            CreateHeaderField("cache-control", "max-age=2592000"), // 37
            CreateHeaderField("cache-control", "max-age=604800"), // 38
            CreateHeaderField("cache-control", "no-cache"), // 39
            CreateHeaderField("cache-control", "no-store"), // 40
            CreateHeaderField("cache-control", "public, max-age=31536000"), // 41
            CreateHeaderField("content-encoding", "br"), // 42
            CreateHeaderField("content-encoding", "gzip"), // 43
            CreateHeaderField("content-type", "application/dns-message"), // 44
            CreateHeaderField("content-type", "application/javascript"), // 45
            CreateHeaderField("content-type", "application/json"), // 46
            CreateHeaderField("content-type", "application/x-www-form-urlencoded"), // 47
            CreateHeaderField("content-type", "image/gif"), // 48
            CreateHeaderField("content-type", "image/jpeg"), // 49
            CreateHeaderField("content-type", "image/png"), // 50
            CreateHeaderField("content-type", "text/css"), // 51
            CreateHeaderField("content-type", "text/html; charset=utf-8"), // 52
            CreateHeaderField("content-type", "text/plain"), // 53
            CreateHeaderField("content-type", "text/plain;charset=utf-8"), // 54
            CreateHeaderField("range", "bytes=0-"), // 55
            CreateHeaderField("strict-transport-security", "max-age=31536000"), // 56
            CreateHeaderField("strict-transport-security", "max-age=31536000; includesubdomains"), // 57; TODO confirm spaces here don't matter?
            CreateHeaderField("strict-transport-security", "max-age=31536000; includesubdomains; preload"), // 58
            CreateHeaderField("vary", "accept-encoding"), // 59
            CreateHeaderField("vary", "origin"), // 60
            CreateHeaderField("x-content-type-options", "nosniff"), // 61
            CreateHeaderField("x-xss-protection", "1; mode=block"), // 62
            CreateHeaderField(":status", "100"), // 63
            CreateHeaderField(":status", "204"), // 64
            CreateHeaderField(":status", "206"), // 65
            CreateHeaderField(":status", "302"), // 66
            CreateHeaderField(":status", "400"), // 67
            CreateHeaderField(":status", "403"), // 68
            CreateHeaderField(":status", "421"), // 69
            CreateHeaderField(":status", "425"), // 70
            CreateHeaderField(":status", "500"), // 71
            CreateHeaderField("accept-language", ""), // 72
            CreateHeaderField("access-control-allow-credentials", "FALSE"), // 73
            CreateHeaderField("access-control-allow-credentials", "TRUE"), // 74
            CreateHeaderField("access-control-allow-headers", "*"), // 75
            CreateHeaderField("access-control-allow-methods", "get"), // 76
            CreateHeaderField("access-control-allow-methods", "get, post, options"), // 77
            CreateHeaderField("access-control-allow-methods", "options"), // 78
            CreateHeaderField("access-control-expose-headers", "content-length"), // 79
            CreateHeaderField("access-control-request-headers", "content-type"), // 80
            CreateHeaderField("access-control-request-method", "get"), // 81
            CreateHeaderField("access-control-request-method", "post"), // 82
            CreateHeaderField("alt-svc", "clear"), // 83
            CreateHeaderField("authorization", ""), // 84
            CreateHeaderField("content-security-policy", "script-src 'none'; object-src 'none'; base-uri 'none'"), // 85
            CreateHeaderField("early-data", "1"), // 86
            CreateHeaderField("expect-ct", ""), // 87
            CreateHeaderField("forwarded", ""), // 88
            CreateHeaderField("if-range", ""), // 89
            CreateHeaderField("origin", ""), // 90
            CreateHeaderField("purpose", "prefetch"), // 91
            CreateHeaderField("server", ""), // 92
            CreateHeaderField("timing-allow-origin", "*"), // 93
            CreateHeaderField("upgrading-insecure-requests", "1"), // 94
            CreateHeaderField("user-agent", ""), // 95
            CreateHeaderField("x-forwarded-for", ""), // 96
            CreateHeaderField("x-frame-options", "deny"), // 97
            CreateHeaderField("x-frame-options", "sameorigin"), // 98
        };

        private static HeaderField CreateHeaderField(string name, string value)
            => new HeaderField(Encoding.ASCII.GetBytes(name), Encoding.ASCII.GetBytes(value));
    }
}
