// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;

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
