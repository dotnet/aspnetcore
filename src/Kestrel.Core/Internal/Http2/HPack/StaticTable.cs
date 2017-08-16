// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

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

        public int Length => _staticTable.Length;

        public HeaderField this[int index] => _staticTable[index];

        public IReadOnlyDictionary<int, int> StatusIndex => _statusIndex;

        private readonly HeaderField[] _staticTable = new HeaderField[]
        {
            new HeaderField(":authority", ""),
            new HeaderField(":method", "GET"),
            new HeaderField(":method", "POST"),
            new HeaderField(":path", "/"),
            new HeaderField(":path", "/index.html"),
            new HeaderField(":scheme", "http"),
            new HeaderField(":scheme", "https"),
            new HeaderField(":status", "200"),
            new HeaderField(":status", "204"),
            new HeaderField(":status", "206"),
            new HeaderField(":status", "304"),
            new HeaderField(":status", "400"),
            new HeaderField(":status", "404"),
            new HeaderField(":status", "500"),
            new HeaderField("accept-charset", ""),
            new HeaderField("accept-encoding", "gzip, deflate"),
            new HeaderField("accept-language", ""),
            new HeaderField("accept-ranges", ""),
            new HeaderField("accept", ""),
            new HeaderField("access-control-allow-origin", ""),
            new HeaderField("age", ""),
            new HeaderField("allow", ""),
            new HeaderField("authorization", ""),
            new HeaderField("cache-control", ""),
            new HeaderField("content-disposition", ""),
            new HeaderField("content-encoding", ""),
            new HeaderField("content-language", ""),
            new HeaderField("content-length", ""),
            new HeaderField("content-location", ""),
            new HeaderField("content-range", ""),
            new HeaderField("content-type", ""),
            new HeaderField("cookie", ""),
            new HeaderField("date", ""),
            new HeaderField("etag", ""),
            new HeaderField("expect", ""),
            new HeaderField("expires", ""),
            new HeaderField("from", ""),
            new HeaderField("host", ""),
            new HeaderField("if-match", ""),
            new HeaderField("if-modified-since", ""),
            new HeaderField("if-none-match", ""),
            new HeaderField("if-range", ""),
            new HeaderField("if-unmodifiedsince", ""),
            new HeaderField("last-modified", ""),
            new HeaderField("link", ""),
            new HeaderField("location", ""),
            new HeaderField("max-forwards", ""),
            new HeaderField("proxy-authenticate", ""),
            new HeaderField("proxy-authorization", ""),
            new HeaderField("range", ""),
            new HeaderField("referer", ""),
            new HeaderField("refresh", ""),
            new HeaderField("retry-after", ""),
            new HeaderField("server", ""),
            new HeaderField("set-cookie", ""),
            new HeaderField("strict-transport-security", ""),
            new HeaderField("transfer-encoding", ""),
            new HeaderField("user-agent", ""),
            new HeaderField("vary", ""),
            new HeaderField("via", ""),
            new HeaderField("www-authenticate", "")
        };
    }
}
