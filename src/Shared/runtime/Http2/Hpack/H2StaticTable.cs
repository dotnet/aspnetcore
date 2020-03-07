// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;

namespace System.Net.Http.HPack
{
    internal static class H2StaticTable
    {
        // Index of status code into s_staticDecoderTable
        private static readonly Dictionary<int, int> s_statusIndex = new Dictionary<int, int>
        {
            [200] = 8,
            [204] = 9,
            [206] = 10,
            [304] = 11,
            [400] = 12,
            [404] = 13,
            [500] = 14,
        };

        public static int Count => s_staticDecoderTable.Length;

        public static HeaderField Get(int index) => s_staticDecoderTable[index];

        public static IReadOnlyDictionary<int, int> StatusIndex => s_statusIndex;

        private static readonly HeaderField[] s_staticDecoderTable = new HeaderField[]
        {
            CreateHeaderField(":authority", ""),
            CreateHeaderField(":method", "GET"),
            CreateHeaderField(":method", "POST"),
            CreateHeaderField(":path", "/"),
            CreateHeaderField(":path", "/index.html"),
            CreateHeaderField(":scheme", "http"),
            CreateHeaderField(":scheme", "https"),
            CreateHeaderField(":status", "200"),
            CreateHeaderField(":status", "204"),
            CreateHeaderField(":status", "206"),
            CreateHeaderField(":status", "304"),
            CreateHeaderField(":status", "400"),
            CreateHeaderField(":status", "404"),
            CreateHeaderField(":status", "500"),
            CreateHeaderField("accept-charset", ""),
            CreateHeaderField("accept-encoding", "gzip, deflate"),
            CreateHeaderField("accept-language", ""),
            CreateHeaderField("accept-ranges", ""),
            CreateHeaderField("accept", ""),
            CreateHeaderField("access-control-allow-origin", ""),
            CreateHeaderField("age", ""),
            CreateHeaderField("allow", ""),
            CreateHeaderField("authorization", ""),
            CreateHeaderField("cache-control", ""),
            CreateHeaderField("content-disposition", ""),
            CreateHeaderField("content-encoding", ""),
            CreateHeaderField("content-language", ""),
            CreateHeaderField("content-length", ""),
            CreateHeaderField("content-location", ""),
            CreateHeaderField("content-range", ""),
            CreateHeaderField("content-type", ""),
            CreateHeaderField("cookie", ""),
            CreateHeaderField("date", ""),
            CreateHeaderField("etag", ""),
            CreateHeaderField("expect", ""),
            CreateHeaderField("expires", ""),
            CreateHeaderField("from", ""),
            CreateHeaderField("host", ""),
            CreateHeaderField("if-match", ""),
            CreateHeaderField("if-modified-since", ""),
            CreateHeaderField("if-none-match", ""),
            CreateHeaderField("if-range", ""),
            CreateHeaderField("if-unmodified-since", ""),
            CreateHeaderField("last-modified", ""),
            CreateHeaderField("link", ""),
            CreateHeaderField("location", ""),
            CreateHeaderField("max-forwards", ""),
            CreateHeaderField("proxy-authenticate", ""),
            CreateHeaderField("proxy-authorization", ""),
            CreateHeaderField("range", ""),
            CreateHeaderField("referer", ""),
            CreateHeaderField("refresh", ""),
            CreateHeaderField("retry-after", ""),
            CreateHeaderField("server", ""),
            CreateHeaderField("set-cookie", ""),
            CreateHeaderField("strict-transport-security", ""),
            CreateHeaderField("transfer-encoding", ""),
            CreateHeaderField("user-agent", ""),
            CreateHeaderField("vary", ""),
            CreateHeaderField("via", ""),
            CreateHeaderField("www-authenticate", "")
        };

        // TODO: The HeaderField constructor will allocate and copy again. We should avoid this.
        // Tackle as part of header table allocation strategy in general (see note in HeaderField constructor).

        private static HeaderField CreateHeaderField(string name, string value) =>
            new HeaderField(
                Encoding.ASCII.GetBytes(name),
                value.Length != 0 ? Encoding.ASCII.GetBytes(value) : Array.Empty<byte>());

        // Values for encoding.
        // Unused values are omitted.
        public const int Authority = 1;
        public const int MethodGet = 2;
        public const int MethodPost = 3;
        public const int PathSlash = 4;
        public const int SchemeHttp = 6;
        public const int SchemeHttps = 7;
        public const int Status200 = 8;
        public const int AcceptCharset = 15;
        public const int AcceptEncoding = 16;
        public const int AcceptLanguage = 17;
        public const int AcceptRanges = 18;
        public const int Accept = 19;
        public const int AccessControlAllowOrigin = 20;
        public const int Age = 21;
        public const int Allow = 22;
        public const int Authorization = 23;
        public const int CacheControl = 24;
        public const int ContentDisposition = 25;
        public const int ContentEncoding = 26;
        public const int ContentLanguage = 27;
        public const int ContentLength = 28;
        public const int ContentLocation = 29;
        public const int ContentRange = 30;
        public const int ContentType = 31;
        public const int Cookie = 32;
        public const int Date = 33;
        public const int ETag = 34;
        public const int Expect = 35;
        public const int Expires = 36;
        public const int From = 37;
        public const int Host = 38;
        public const int IfMatch = 39;
        public const int IfModifiedSince = 40;
        public const int IfNoneMatch = 41;
        public const int IfRange = 42;
        public const int IfUnmodifiedSince = 43;
        public const int LastModified = 44;
        public const int Link = 45;
        public const int Location = 46;
        public const int MaxForwards = 47;
        public const int ProxyAuthenticate = 48;
        public const int ProxyAuthorization = 49;
        public const int Range = 50;
        public const int Referer = 51;
        public const int Refresh = 52;
        public const int RetryAfter = 53;
        public const int Server = 54;
        public const int SetCookie = 55;
        public const int StrictTransportSecurity = 56;
        public const int TransferEncoding = 57;
        public const int UserAgent = 58;
        public const int Vary = 59;
        public const int Via = 60;
        public const int WwwAuthenticate = 61;
    }
}
