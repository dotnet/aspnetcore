// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// A middleware for handling exceptions in the application.
/// </summary>
public class ExceptionHandlerMiddleware
{
    private readonly ExceptionHandlerMiddlewareImpl _innerMiddlewareImpl;

    /// <summary>
    /// Creates a new <see cref="ExceptionHandlerMiddleware"/>
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
    /// <param name="options">The options for configuring the middleware.</param>
    /// <param name="diagnosticListener">The <see cref="DiagnosticListener"/> used for writing diagnostic messages.</param>
    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILoggerFactory loggerFactory,
        IOptions<ExceptionHandlerOptions> options,
        DiagnosticListener diagnosticListener)
    {
        _innerMiddlewareImpl = new(
            next,
            loggerFactory,
            options,
            diagnosticListener,
            Enumerable.Empty<IExceptionHandler>(),
            new DummyMeterFactory(),
            problemDetailsService: null);
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    public Task Invoke(HttpContext context)
        => _innerMiddlewareImpl.Invoke(context);
}
