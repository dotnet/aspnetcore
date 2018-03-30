// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Features;

namespace Microsoft.AspNetCore.Http.Connections
{
    public static class DefaultConnectionContextExtensions
    {
        public static HttpContext GetHttpContext(this ConnectionContext connection)
        {
            return connection.Features.Get<IHttpContextFeature>()?.HttpContext;
        }

        public static void SetHttpContext(this ConnectionContext connection, HttpContext httpContext)
        {
            var feature = connection.Features.Get<IHttpContextFeature>();
            if (feature == null)
            {
                feature = new HttpContextFeature();
                connection.Features.Set(feature);
            }
            feature.HttpContext = httpContext;
        }
    }
}
