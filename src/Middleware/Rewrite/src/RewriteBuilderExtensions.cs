// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
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
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

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
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        // put middleware in pipeline
        return AddRewriteMiddleware(app, Options.Create(options));
    }

    private static IApplicationBuilder AddRewriteMiddleware(IApplicationBuilder app, IOptions<RewriteOptions>? options)
    {
        const string globalRouteBuilderKey = "__GlobalEndpointRouteBuilder";
        // Only use this path if there's a global router (in the 'WebApplication' case).
        if (app.Properties.TryGetValue(globalRouteBuilderKey, out var routeBuilder) && routeBuilder is not null)
        {
            return app.Use(next =>
            {
                if (options is null)
                {
                    options = app.ApplicationServices.GetRequiredService<IOptions<RewriteOptions>>();
                }

                var webHostEnv = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();

                // start a new middleware pipeline
                var builder = app.New();
                // use the old routing pipeline if it exists so we preserve all the routes and matching logic
                // ((IApplicationBuilder)WebApplication).New() does not copy globalRouteBuilderKey automatically like it does for all other properties.
                builder.Properties[globalRouteBuilderKey] = routeBuilder;
                builder.UseRouting();
                // apply the next middleware
                builder.Run(next);
                options.Value.BranchedNext = builder.Build();

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
