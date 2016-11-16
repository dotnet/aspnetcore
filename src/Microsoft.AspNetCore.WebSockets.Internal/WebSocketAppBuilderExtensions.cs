// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using Microsoft.AspNetCore.WebSockets.Internal;

namespace Microsoft.AspNetCore.Builder
{
    public static class WebSocketAppBuilderExtensions
    {
        public static void UseWebSocketConnections(this IApplicationBuilder app)
        {
            // Only the GC can clean up this channel factory :(
            app.UseWebSocketConnections(new PipelineFactory(), new WebSocketConnectionOptions());
        }

        public static void UseWebSocketConnections(this IApplicationBuilder app, PipelineFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            app.UseWebSocketConnections(factory, new WebSocketConnectionOptions());
        }

        public static void UseWebSocketConnections(this IApplicationBuilder app, PipelineFactory factory, WebSocketConnectionOptions options)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            app.UseMiddleware<WebSocketConnectionMiddleware>(factory, options);
        }
    }
}
