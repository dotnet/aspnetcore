// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.WebSockets.Server;

namespace Microsoft.AspNet.Builder
{
    public static class WebSocketMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebSockets(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.UseWebSockets(options => { });
        }

        public static IApplicationBuilder UseWebSockets(this IApplicationBuilder builder, Action<WebSocketOptions> configureOptions)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new WebSocketOptions();
            configureOptions(options);

            return builder.Use(next => new WebSocketMiddleware(next, options).Invoke);
        }
    }
}