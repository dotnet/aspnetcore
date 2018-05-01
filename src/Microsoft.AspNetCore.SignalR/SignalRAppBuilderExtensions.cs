// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class SignalRAppBuilderExtensions
    {
        /// <summary>
        /// Adds SignalR to the <see cref="IApplicationBuilder"/> request execution pipeline.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="configure">A callback to configure hub routes.</param>
        /// <returns>The same instance of the <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseSignalR(this IApplicationBuilder app, Action<HubRouteBuilder> configure)
        {
            app.UseConnections(routes =>
            {
                configure(new HubRouteBuilder(routes));
            });

            return app;
        }
    }
}
