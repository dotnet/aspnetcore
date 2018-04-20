// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace Microsoft.AspNetCore.SignalR
{
    public static class GetHttpContextExtensions
    {
        public static HttpContext GetHttpContext(this HubCallerContext connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return connection.Features.Get<IHttpContextFeature>()?.HttpContext;
        }

        public static HttpContext GetHttpContext(this HubConnectionContext connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            return connection.Features.Get<IHttpContextFeature>()?.HttpContext;
        }
    }
}
