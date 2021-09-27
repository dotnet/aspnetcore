// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Versioning;

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
        [RequiresPreviewFeatures("Kestrel HTTP/3 support for .NET 6 is in preview.", Url = "https://aka.ms/aspnet/kestrel/http3reqs")]
        Http3 = 0x4,
        [RequiresPreviewFeatures("Kestrel HTTP/3 support for .NET 6 is in preview.", Url = "https://aka.ms/aspnet/kestrel/http3reqs")]
        Http1AndHttp2AndHttp3 = Http1 | Http2 | Http3
    }
}
