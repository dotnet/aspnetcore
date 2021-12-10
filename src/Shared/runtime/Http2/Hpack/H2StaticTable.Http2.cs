// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Net.Http.HPack
{
    internal static partial class H2StaticTable
    {
        public static int Count => s_staticDecoderTable.Length;

        public static ref readonly HeaderField Get(int index) => ref s_staticDecoderTable[index];

        public static bool TryGetStatusIndex(int status, out int index)
        {
            index = status switch
            {
                200 => 8,
                204 => 9,
                206 => 10,
                304 => 11,
                400 => 12,
                404 => 13,
                500 => 14,
                _ => -1
            };

            return index != -1;
        }

        private static readonly HeaderField[] s_staticDecoderTable = new HeaderField[]
        {
            CreateHeaderField(1, ":authority", ""),
            CreateHeaderField(2, ":method", "GET"),
            CreateHeaderField(3, ":method", "POST"),
            CreateHeaderField(4, ":path", "/"),
            CreateHeaderField(5, ":path", "/index.html"),
            CreateHeaderField(6, ":scheme", "http"),
            CreateHeaderField(7, ":scheme", "https"),
            CreateHeaderField(8, ":status", "200"),
            CreateHeaderField(9, ":status", "204"),
            CreateHeaderField(10, ":status", "206"),
            CreateHeaderField(11, ":status", "304"),
            CreateHeaderField(12, ":status", "400"),
            CreateHeaderField(13, ":status", "404"),
            CreateHeaderField(14, ":status", "500"),
            CreateHeaderField(15, "accept-charset", ""),
            CreateHeaderField(16, "accept-encoding", "gzip, deflate"),
            CreateHeaderField(17, "accept-language", ""),
            CreateHeaderField(18, "accept-ranges", ""),
            CreateHeaderField(19, "accept", ""),
            CreateHeaderField(20, "access-control-allow-origin", ""),
            CreateHeaderField(21, "age", ""),
            CreateHeaderField(22, "allow", ""),
            CreateHeaderField(23, "authorization", ""),
            CreateHeaderField(24, "cache-control", ""),
            CreateHeaderField(25, "content-disposition", ""),
            CreateHeaderField(26, "content-encoding", ""),
            CreateHeaderField(27, "content-language", ""),
            CreateHeaderField(28, "content-length", ""),
            CreateHeaderField(29, "content-location", ""),
            CreateHeaderField(30, "content-range", ""),
            CreateHeaderField(31, "content-type", ""),
            CreateHeaderField(32, "cookie", ""),
            CreateHeaderField(33, "date", ""),
            CreateHeaderField(34, "etag", ""),
            CreateHeaderField(35, "expect", ""),
            CreateHeaderField(36, "expires", ""),
            CreateHeaderField(37, "from", ""),
            CreateHeaderField(38, "host", ""),
            CreateHeaderField(39, "if-match", ""),
            CreateHeaderField(40, "if-modified-since", ""),
            CreateHeaderField(41, "if-none-match", ""),
            CreateHeaderField(42, "if-range", ""),
            CreateHeaderField(43, "if-unmodified-since", ""),
            CreateHeaderField(44, "last-modified", ""),
            CreateHeaderField(45, "link", ""),
            CreateHeaderField(46, "location", ""),
            CreateHeaderField(47, "max-forwards", ""),
            CreateHeaderField(48, "proxy-authenticate", ""),
            CreateHeaderField(49, "proxy-authorization", ""),
            CreateHeaderField(50, "range", ""),
            CreateHeaderField(51, "referer", ""),
            CreateHeaderField(52, "refresh", ""),
            CreateHeaderField(53, "retry-after", ""),
            CreateHeaderField(54, "server", ""),
            CreateHeaderField(55, "set-cookie", ""),
            CreateHeaderField(56, "strict-transport-security", ""),
            CreateHeaderField(57, "transfer-encoding", ""),
            CreateHeaderField(58, "user-agent", ""),
            CreateHeaderField(59, "vary", ""),
            CreateHeaderField(60, "via", ""),
            CreateHeaderField(61, "www-authenticate", "")
        };

        // TODO: The HeaderField constructor will allocate and copy again. We should avoid this.
        // Tackle as part of header table allocation strategy in general (see note in HeaderField constructor).

        private static HeaderField CreateHeaderField(int staticTableIndex, string name, string value) =>
            new HeaderField(
                staticTableIndex,
                Encoding.ASCII.GetBytes(name),
                value.Length != 0 ? Encoding.ASCII.GetBytes(value) : Array.Empty<byte>());
    }
}
