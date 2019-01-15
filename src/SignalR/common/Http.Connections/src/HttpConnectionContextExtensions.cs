// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace Microsoft.AspNetCore.Http.Connections
{
    public static class HttpConnectionContextExtensions
    {
        public static HttpContext GetHttpContext(this ConnectionContext connection)
        {
            return connection.Features.Get<IHttpContextFeature>()?.HttpContext;
        }
    }
}
