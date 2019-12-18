// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// IApplicationBuilder extension methods to add and configure WebSocketMiddleware.
    /// </summary>
    public static class WebSocketMiddlewareExtensions
    {
        /// <summary>
        /// Adds the WebSocketMiddleware to the request pipeline.
        /// </summary>
        /// <param name="app">
        /// The Microsoft.AspNetCore.Builder.IApplicationBuilder to configure.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Builder.IApplicationBuilder.
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
        /// Adds the WebSocketMiddleware to the request pipeline.
        /// </summary>
        /// <param name="app">
        /// The Microsoft.AspNetCore.Builder.IApplicationBuilder to configure.
        /// </param>
        /// <param name="options">
        /// The WebSocketOptions to be used for the WebSocketMiddleware.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Builder.IApplicationBuilder.
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
