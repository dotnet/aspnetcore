// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Sockets.Http.Features;

namespace Microsoft.AspNetCore.Sockets
{
    public static class HttpConnectionContextExtensions
    {
        public static HttpContext GetHttpContext(this ConnectionContext connection)
        {
            return connection.Features.Get<IHttpContextFeature>().HttpContext;
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
