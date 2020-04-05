// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public enum HttpVersion
    {
        Unknown = -1,
        Http10 = 0,
        Http11 = 1,
        Http2 = 2,
        Http3 = 3
    }
}
