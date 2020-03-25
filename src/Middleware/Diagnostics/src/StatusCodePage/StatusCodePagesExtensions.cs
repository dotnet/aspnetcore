// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
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
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

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
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

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
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

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
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

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
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (locationFormat.StartsWith("~"))
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
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

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
            string queryFormat = null)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseStatusCodePages(async context =>
            {
                var newPath = new PathString(
                    string.Format(CultureInfo.InvariantCulture, pathFormat, context.HttpContext.Response.StatusCode));
                var formatedQueryString = queryFormat == null ? null :
                    string.Format(CultureInfo.InvariantCulture, queryFormat, context.HttpContext.Response.StatusCode);
                var newQueryString = queryFormat == null ? QueryString.Empty : new QueryString(formatedQueryString);

                var originalPath = context.HttpContext.Request.Path;
                var originalQueryString = context.HttpContext.Request.QueryString;
                // Store the original paths so the app can check it.
                context.HttpContext.Features.Set<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature()
                {
                    OriginalPathBase = context.HttpContext.Request.PathBase.Value,
                    OriginalPath = originalPath.Value,
                    OriginalQueryString = originalQueryString.HasValue ? originalQueryString.Value : null,
                });

                // An endpoint may have already been set. Since we're going to re-invoke the middleware pipeline we need to reset
                // the endpoint and route values to ensure things are re-calculated.
                context.HttpContext.SetEndpoint(endpoint: null);
                var routeValuesFeature = context.HttpContext.Features.Get<IRouteValuesFeature>();
                routeValuesFeature?.RouteValues?.Clear();

                context.HttpContext.Request.Path = newPath;
                context.HttpContext.Request.QueryString = newQueryString;
                try
                {
                    await context.Next(context.HttpContext);
                }
                finally
                {
                    context.HttpContext.Request.QueryString = originalQueryString;
                    context.HttpContext.Request.Path = originalPath;
                    context.HttpContext.Features.Set<IStatusCodeReExecuteFeature>(null);
                }
            });
        }
    }
}
