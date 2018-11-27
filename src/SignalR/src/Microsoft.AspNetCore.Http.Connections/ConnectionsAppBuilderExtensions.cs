// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class ConnectionsAppBuilderExtensions
    {
        /// <summary>
        /// Adds support for ASP.NET Core Connection Handlers to the <see cref="IApplicationBuilder"/> request execution pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="configure">A callback to configure connection routes.</param>
        /// <returns>The same instance of the <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseConnections(this IApplicationBuilder app, Action<ConnectionsRouteBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var dispatcher = app.ApplicationServices.GetRequiredService<HttpConnectionDispatcher>();

            var routes = new RouteBuilder(app);

            configure(new ConnectionsRouteBuilder(routes, dispatcher));

            app.UseWebSockets();
            app.UseRouter(routes.Build());
            return app;
        }
    }
}
