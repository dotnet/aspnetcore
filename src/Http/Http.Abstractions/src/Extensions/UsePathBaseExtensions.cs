// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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
        // Only use this path if there's a global router (in the 'WebApplication' case).
        if (app.Properties.TryGetValue(globalRouteBuilderKey, out var routeBuilder) && routeBuilder is not null)
        {
            return app.Use(next =>
            {
                var newNext = ReRouteHelper.ReRoute(app, routeBuilder, next);
                return new UsePathBaseMiddleware(newNext, pathBase).Invoke;
            });
        }

        return app.UseMiddleware<UsePathBaseMiddleware>(pathBase);
    }
}
