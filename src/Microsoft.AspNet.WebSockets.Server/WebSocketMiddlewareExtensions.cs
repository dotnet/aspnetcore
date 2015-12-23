// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.WebSockets.Server;

namespace Microsoft.AspNet.Builder
{
    public static class WebSocketMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSockets(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseWebSockets(options => { });
        }

        public static IApplicationBuilder UseWebSockets(this IApplicationBuilder app, Action<WebSocketOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new WebSocketOptions();
            configureOptions(options);

            return app.UseMiddleware<WebSocketMiddleware>(options);
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

            return app.UseMiddleware<WebSocketMiddleware>(options);
        }
    }
}