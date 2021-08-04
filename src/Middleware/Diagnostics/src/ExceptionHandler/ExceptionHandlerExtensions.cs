// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for enabling <see cref="ExceptionHandlerExtensions"/>.
    /// </summary>
    public static class ExceptionHandlerExtensions
    {
        /// <summary>
        /// Adds a middleware to the pipeline that will catch exceptions, log them, and re-execute the request in an alternate pipeline.
        /// The request will not be re-executed if the response has already started.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<ExceptionHandlerMiddleware>();
        }

        /// <summary>
        /// Adds a middleware to the pipeline that will catch exceptions, log them, reset the request path, and re-execute the request.
        /// The request will not be re-executed if the response has already started.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="errorHandlingPath"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app, string errorHandlingPath)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandlingPath = new PathString(errorHandlingPath)
            });
        }

        /// <summary>
        /// Adds a middleware to the pipeline that will catch exceptions, log them, and re-execute the request in an alternate pipeline.
        /// The request will not be re-executed if the response has already started.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app, Action<IApplicationBuilder> configure)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var subAppBuilder = app.New();
            configure(subAppBuilder);
            var exceptionHandlerPipeline = subAppBuilder.Build();

            return app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = exceptionHandlerPipeline
            });
        }

        /// <summary>
        /// Adds a middleware to the pipeline that will catch exceptions, log them, and re-execute the request in an alternate pipeline.
        /// The request will not be re-executed if the response has already started.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app, ExceptionHandlerOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // UseRouting called before this middleware or Minimal
            if (app.Properties.ContainsKey("__EndpointRouteBuilder") || app.Properties.ContainsKey("__WebApplicationBuilder"))
            {
                return app.Use(next =>
                {
                    var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
                    var diagnosticListener = app.ApplicationServices.GetRequiredService<DiagnosticListener>();

                    if (!string.IsNullOrEmpty(options.ExceptionHandlingPath) && options.ExceptionHandler is null)
                    {
                        var errorBuilder = app.New();
                        errorBuilder.UseRouting(overrideEndpointRouteBuilder: false);
                        errorBuilder.Run(next);
                        options.ExceptionHandler = errorBuilder.Build();
                    }

                    return new ExceptionHandlerMiddleware(next, loggerFactory, Options.Create(options), diagnosticListener).Invoke;
                });
            }
            else
            {
                return app.UseMiddleware<ExceptionHandlerMiddleware>(Options.Create(options));
            }
        }
    }
}
