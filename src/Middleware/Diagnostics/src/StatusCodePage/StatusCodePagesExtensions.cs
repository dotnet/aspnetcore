// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for enabling <see cref="StatusCodePagesMiddleware"/>.
/// </summary>
public static class StatusCodePagesExtensions
{
    /// <summary>
    /// Adds a StatusCodePages middleware with the given options that checks for responses with status codes
    /// between 400 and 599 that do not have a body.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, StatusCodePagesOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        return app.UseMiddleware<StatusCodePagesMiddleware>(Options.Create(options));
    }

    /// <summary>
    /// Adds a StatusCodePages middleware with a default response handler that checks for responses with status codes
    /// between 400 and 599 that do not have a body.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<StatusCodePagesMiddleware>();
    }

    /// <summary>
    /// Adds a StatusCodePages middleware with the specified handler that checks for responses with status codes
    /// between 400 and 599 that do not have a body.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, Func<StatusCodeContext, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(handler);

        return app.UseStatusCodePages(new StatusCodePagesOptions
        {
            HandleAsync = handler
        });
    }

    /// <summary>
    /// Adds a StatusCodePages middleware with the specified response body to send. This may include a '{0}' placeholder for the status code.
    /// The middleware checks for responses with status codes between 400 and 599 that do not have a body.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="contentType"></param>
    /// <param name="bodyFormat"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, string contentType, string bodyFormat)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseStatusCodePages(context =>
        {
            var body = string.Format(CultureInfo.InvariantCulture, bodyFormat, context.HttpContext.Response.StatusCode);
            context.HttpContext.Response.ContentType = contentType;
            return context.HttpContext.Response.WriteAsync(body);
        });
    }

    /// <summary>
    /// Adds a StatusCodePages middleware to the pipeline. Specifies that responses should be handled by redirecting
    /// with the given location URL template. This may include a '{0}' placeholder for the status code. URLs starting
    /// with '~' will have PathBase prepended, where any other URL will be used as is.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="locationFormat"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseStatusCodePagesWithRedirects(this IApplicationBuilder app, string locationFormat)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (locationFormat.StartsWith('~'))
        {
            locationFormat = locationFormat.Substring(1);
            return app.UseStatusCodePages(context =>
            {
                var location = string.Format(CultureInfo.InvariantCulture, locationFormat, context.HttpContext.Response.StatusCode);
                context.HttpContext.Response.Redirect(context.HttpContext.Request.PathBase + location);
                return Task.CompletedTask;
            });
        }
        else
        {
            return app.UseStatusCodePages(context =>
            {
                var location = string.Format(CultureInfo.InvariantCulture, locationFormat, context.HttpContext.Response.StatusCode);
                context.HttpContext.Response.Redirect(location);
                return Task.CompletedTask;
            });
        }
    }

    /// <summary>
    /// Adds a StatusCodePages middleware to the pipeline with the specified alternate middleware pipeline to execute
    /// to generate the response body.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseStatusCodePages(this IApplicationBuilder app, Action<IApplicationBuilder> configuration)
    {
        ArgumentNullException.ThrowIfNull(app);

        var builder = app.New();
        configuration(builder);
        var tangent = builder.Build();
        return app.UseStatusCodePages(context => tangent(context.HttpContext));
    }

    /// <summary>
    /// Adds a StatusCodePages middleware to the pipeline. Specifies that the response body should be generated by
    /// re-executing the request pipeline using an alternate path. This path may contain a '{0}' placeholder of the status code.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="pathFormat"></param>
    /// <param name="queryFormat"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseStatusCodePagesWithReExecute(
        this IApplicationBuilder app,
        string pathFormat,
        string? queryFormat = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Only use this path if there's a global router (in the 'WebApplication' case).
        if (app.Properties.TryGetValue(RerouteHelper.GlobalRouteBuilderKey, out var routeBuilder) && routeBuilder is not null)
        {
            return app.Use(next =>
            {
                var newNext = RerouteHelper.Reroute(app, routeBuilder, next);
                return new StatusCodePagesMiddleware(next,
                    Options.Create(new StatusCodePagesOptions() { HandleAsync = CreateHandler(pathFormat, queryFormat, newNext) })).Invoke;
            });
        }

        return app.UseStatusCodePages(CreateHandler(pathFormat, queryFormat));
    }

    private static Func<StatusCodeContext, Task> CreateHandler(string pathFormat, string? queryFormat, RequestDelegate? next = null)
    {
        var handler = async (StatusCodeContext context) =>
        {
            var originalStatusCode = context.HttpContext.Response.StatusCode;

            var newPath = new PathString(
                string.Format(CultureInfo.InvariantCulture, pathFormat, originalStatusCode));
            var formatedQueryString = queryFormat == null ? null :
                string.Format(CultureInfo.InvariantCulture, queryFormat, originalStatusCode);
            var newQueryString = queryFormat == null ? QueryString.Empty : new QueryString(formatedQueryString);

            var originalPath = context.HttpContext.Request.Path;
            var originalQueryString = context.HttpContext.Request.QueryString;

            var routeValuesFeature = context.HttpContext.Features.Get<IRouteValuesFeature>();

            // Store the original paths so the app can check it.
            context.HttpContext.Features.Set<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature()
            {
                OriginalPathBase = context.HttpContext.Request.PathBase.Value!,
                OriginalPath = originalPath.Value!,
                OriginalQueryString = originalQueryString.HasValue ? originalQueryString.Value : null,
                OriginalStatusCode = originalStatusCode,
                Endpoint = context.HttpContext.GetEndpoint(),
                RouteValues = routeValuesFeature?.RouteValues
            });

            // An endpoint may have already been set. Since we're going to re-invoke the middleware pipeline we need to reset
            // the endpoint and route values to ensure things are re-calculated.
            HttpExtensions.ClearEndpoint(context.HttpContext);

            context.HttpContext.Request.Path = newPath;
            context.HttpContext.Request.QueryString = newQueryString;
            try
            {
                if (next is not null)
                {
                    await next(context.HttpContext);
                }
                else
                {
                    await context.Next(context.HttpContext);
                }
            }
            finally
            {
                context.HttpContext.Request.QueryString = originalQueryString;
                context.HttpContext.Request.Path = originalPath;
                context.HttpContext.Features.Set<IStatusCodeReExecuteFeature?>(null);
            }
        };

        return handler;
    }
}
