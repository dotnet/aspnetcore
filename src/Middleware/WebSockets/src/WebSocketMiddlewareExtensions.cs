// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    public static class WebSocketMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSockets(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<WebSocketMiddleware>();
        }

        public static IApplicationBuilder UseWebSockets(this IApplicationBuilder app, WebSocketOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<WebSocketMiddleware>(Options.Create(options));
        }
    }
}