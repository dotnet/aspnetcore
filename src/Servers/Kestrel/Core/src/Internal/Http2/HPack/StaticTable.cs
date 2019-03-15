// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    public class StaticTable
    {
        private static readonly StaticTable _instance = new StaticTable();

        private readonly Dictionary<int, int> _statusIndex = new Dictionary<int, int>
        {
            [200] = 8,
            [204] = 9,
            [206] = 10,
            [304] = 11,
            [400] = 12,
            [404] = 13,
            [500] = 14,
        };

        private StaticTable()
        {
        }

        public static StaticTable Instance => _instance;

        public int Count => _staticTable.Length;

        public HeaderField this[int index] => _staticTable[index];

        public IReadOnlyDictionary<int, int> StatusIndex => _statusIndex;

        private readonly HeaderField[] _staticTable = new HeaderField[]
        {
            CreateHeaderField(HeaderNames.Authority, ""),
            CreateHeaderField(HeaderNames.Method, "GET"),
            CreateHeaderField(HeaderNames.Method, "POST"),
            CreateHeaderField(HeaderNames.Path, "/"),
            CreateHeaderField(HeaderNames.Path, "/index.html"),
            CreateHeaderField(HeaderNames.Scheme, "http"),
            CreateHeaderField(HeaderNames.Scheme, "https"),
            CreateHeaderField(HeaderNames.Status, "200"),
            CreateHeaderField(HeaderNames.Status, "204"),
            CreateHeaderField(HeaderNames.Status, "206"),
            CreateHeaderField(HeaderNames.Status, "304"),
            CreateHeaderField(HeaderNames.Status, "400"),
            CreateHeaderField(HeaderNames.Status, "404"),
            CreateHeaderField(HeaderNames.Status, "500"),
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
            CreateHeaderField("if-unmodifiedsince", ""),
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

        private static HeaderField CreateHeaderField(string name, string value)
            => new HeaderField(Encoding.ASCII.GetBytes(name), Encoding.ASCII.GetBytes(value));
    }
}
