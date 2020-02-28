// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// <see cref="IApplicationBuilder" /> extension methods to add and configure <see cref="WebSocketMiddleware" />.
    /// </summary>
    public static class WebSocketMiddlewareExtensions
    {
        /// <summary>
        /// Adds the <see cref="WebSocketMiddleware" /> to the request pipeline.
        /// </summary>
        /// <param name="app">
        /// The <see cref="IApplicationBuilder" /> to configure.
        /// </param>
        /// <returns>
        /// The <see cref="IApplicationBuilder" />.
        /// </returns>
        public static IApplicationBuilder UseWebSockets(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<WebSocketMiddleware>();
        }

        /// <summary>
        /// Adds the <see cref="WebSocketMiddleware" /> to the request pipeline.
        /// </summary>
        /// <param name="app">
        /// The <see cref="IApplicationBuilder" /> to configure.
        /// </param>
        /// <param name="options">
        /// The <see cref="WebSocketOptions" /> to be used for the <see cref="WebSocketMiddleware" />.
        /// </param>
        /// <returns>
        /// The <see cref="IApplicationBuilder" />.
        /// </returns>
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
