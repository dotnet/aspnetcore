// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    /// <summary>
    /// HTTP protocol versions
    /// </summary>
    [Flags]
    public enum HttpProtocols
    {
        None = 0x0,
        Http1 = 0x1,
        Http2 = 0x2,
        Http1AndHttp2 = Http1 | Http2,
        Http3 = 0x4,
        Http1AndHttp2AndHttp3 = Http1 | Http2 | Http3
    }
}
