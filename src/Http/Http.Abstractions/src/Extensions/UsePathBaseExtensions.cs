// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/>.
/// </summary>
public static class UsePathBaseExtensions
{
    /// <summary>
    /// Adds a middleware that extracts the specified path base from request path and postpend it to the request path base.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="pathBase">The path base to extract.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
    public static IApplicationBuilder UsePathBase(this IApplicationBuilder app, PathString pathBase)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        // Strip trailing slashes
        pathBase = new PathString(pathBase.Value?.TrimEnd('/'));
        if (!pathBase.HasValue)
        {
            return app;
        }

        const string globalRouteBuilderKey = "__GlobalEndpointRouteBuilder";
        const string useRoutingKey = "__UseRouting";
        // Only use this path if there's a global router (in the 'WebApplication' case).
        if (app.Properties.TryGetValue(globalRouteBuilderKey, out var routeBuilder) && routeBuilder is not null)
        {
            return app.Use(next =>
            {
                // start a new middleware pipeline
                var builder = app.New();
                // use the old routing pipeline if it exists so we preserve all the routes and matching logic
                // ((IApplicationBuilder)WebApplication).New() does not copy globalRouteBuilderKey automatically like it does for all other properties.
                builder.Properties[globalRouteBuilderKey] = routeBuilder;
                // UseRouting()
                if (builder.Properties[useRoutingKey] is Func<IApplicationBuilder, IApplicationBuilder> useRouting)
                {
                    useRouting(builder);
                }
                // apply the next middleware
                builder.Run(next);

                return new UsePathBaseMiddleware(builder.Build(), pathBase).Invoke;
            });
        }

        return app.UseMiddleware<UsePathBaseMiddleware>(pathBase);
    }
}
