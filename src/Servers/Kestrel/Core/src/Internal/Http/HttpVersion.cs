// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public enum HttpVersion : sbyte
    {
        Unknown = -1,
        Http10 = 0,
        Http11 = 1,
        Http2 = 2,
        Http3 = 3
    }
}
