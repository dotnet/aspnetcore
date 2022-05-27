// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

#nullable enable

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal partial class Http2Connection
    {
        // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static
     
        private static ReadOnlySpan<byte> ClientPrefaceBytes => "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"u8;
        private static ReadOnlySpan<byte> AuthorityBytes => ":authority"u8;
        private static ReadOnlySpan<byte> MethodBytes => ":method"u8;
        private static ReadOnlySpan<byte> PathBytes => ":path"u8;
        private static ReadOnlySpan<byte> SchemeBytes => ":scheme"u8;
        private static ReadOnlySpan<byte> StatusBytes => ":status"u8;
        private static ReadOnlySpan<byte> ConnectionBytes => "connection"u8;
        private static ReadOnlySpan<byte> TeBytes => "te"u8;
        private static ReadOnlySpan<byte> TrailersBytes => "trailers"u8;
        private static ReadOnlySpan<byte> ConnectBytes => "CONNECT"u8;
        private static ReadOnlySpan<byte> ProtocolBytes => ":protocol"u8;
    }
}
