// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

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
        ArgumentNullException.ThrowIfNull(app);

        return SetExceptionHandlerMiddleware(app, options: null);
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
        ArgumentNullException.ThrowIfNull(app);

        return app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            ExceptionHandlingPath = new PathString(errorHandlingPath)
        });
    }

    /// <summary>
    /// Adds a middleware to the pipeline that will catch exceptions, log them, reset the request path, and re-execute the request.
    /// The request will not be re-executed if the response has already started.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    /// <param name="errorHandlingPath">The <see cref="string"/> path to the endpoint that will handle the exception.</param>
    /// <param name="createScopeForErrors">Whether or not to create a new <see cref="IServiceProvider"/> scope.</param>
    /// <returns></returns>
    public static IApplicationBuilder UseExceptionHandler(this IApplicationBuilder app, string errorHandlingPath, bool createScopeForErrors)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            ExceptionHandlingPath = new PathString(errorHandlingPath),
            CreateScopeForErrors = createScopeForErrors
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
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configure);

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
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        var iOptions = Options.Create(options);
        return SetExceptionHandlerMiddleware(app, iOptions);
    }

    private static IApplicationBuilder SetExceptionHandlerMiddleware(IApplicationBuilder app, IOptions<ExceptionHandlerOptions>? options)
    {
        var problemDetailsService = app.ApplicationServices.GetService<IProblemDetailsService>();

        app.Properties["analysis.NextMiddlewareName"] = "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware";

        // Only use this path if there's a global router (in the 'WebApplication' case).
        if (app.Properties.TryGetValue(RerouteHelper.GlobalRouteBuilderKey, out var routeBuilder) && routeBuilder is not null)
        {
            return app.Use(next =>
            {
                var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
                var diagnosticListener = app.ApplicationServices.GetRequiredService<DiagnosticListener>();
                var exceptionHandlers = app.ApplicationServices.GetRequiredService<IEnumerable<IExceptionHandler>>();
                var meterFactory = app.ApplicationServices.GetRequiredService<IMeterFactory>();

                if (options is null)
                {
                    options = app.ApplicationServices.GetRequiredService<IOptions<ExceptionHandlerOptions>>();
                }

                if (!string.IsNullOrEmpty(options.Value.ExceptionHandlingPath) && options.Value.ExceptionHandler is null)
                {
                    var newNext = RerouteHelper.Reroute(app, routeBuilder, next);
                    // store the pipeline for the error case
                    options.Value.ExceptionHandler = newNext;
                }

                return new ExceptionHandlerMiddlewareImpl(next, loggerFactory, options, diagnosticListener, exceptionHandlers, meterFactory, problemDetailsService).Invoke;
            });
        }

        if (options is null)
        {
            return app.UseMiddleware<ExceptionHandlerMiddlewareImpl>();
        }

        return app.UseMiddleware<ExceptionHandlerMiddlewareImpl>(options);
    }
}
