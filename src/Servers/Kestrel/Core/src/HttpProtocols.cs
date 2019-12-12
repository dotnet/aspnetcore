// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
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
