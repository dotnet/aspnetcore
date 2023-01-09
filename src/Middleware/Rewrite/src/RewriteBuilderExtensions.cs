// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the <see cref="RewriteMiddleware"/>
/// </summary>
public static class RewriteBuilderExtensions
{
    /// <summary>
    /// Checks if a given Url matches rules and conditions, and modifies the HttpContext on match.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRewriter(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return AddRewriteMiddleware(app, options: null);
    }

    /// <summary>
    /// Checks if a given Url matches rules and conditions, and modifies the HttpContext on match.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/></param>
    /// <param name="options">Options for rewrite.</param>
    /// <returns></returns>
    public static IApplicationBuilder UseRewriter(this IApplicationBuilder app, RewriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        // put middleware in pipeline
        return AddRewriteMiddleware(app, Options.Create(options));
    }

    private static IApplicationBuilder AddRewriteMiddleware(IApplicationBuilder app, IOptions<RewriteOptions>? options)
    {
        // Only use this path if there's a global router (in the 'WebApplication' case).
        if (app.Properties.TryGetValue(RerouteHelper.GlobalRouteBuilderKey, out var routeBuilder) && routeBuilder is not null)
        {
            return app.Use(next =>
            {
                if (options is null)
                {
                    options = app.ApplicationServices.GetRequiredService<IOptions<RewriteOptions>>();
                }

                var webHostEnv = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

                var newNext = RerouteHelper.Reroute(app, routeBuilder, next);
                options.Value.BranchedNext = newNext;

                return new RewriteMiddleware(next, webHostEnv, loggerFactory, options).Invoke;
            });
        }

        if (options is null)
        {
            return app.UseMiddleware<RewriteMiddleware>();
        }

        return app.UseMiddleware<RewriteMiddleware>(options);
    }
}
