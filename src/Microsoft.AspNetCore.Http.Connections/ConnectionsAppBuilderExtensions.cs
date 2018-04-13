// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class ConnectionsAppBuilderExtensions
    {
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
