// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Captures synchronous and asynchronous exceptions from the pipeline and generates error responses.
/// </summary>
public class DeveloperExceptionPageMiddleware
{
    private readonly DeveloperExceptionPageMiddlewareImpl _innerMiddlewareImpl;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeveloperExceptionPageMiddleware"/> class
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="options">The options for configuring the middleware.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
    /// <param name="hostingEnvironment"></param>
    /// <param name="diagnosticSource">The <see cref="DiagnosticSource"/> used for writing diagnostic messages.</param>
    /// <param name="filters">The list of registered <see cref="IDeveloperPageExceptionFilter"/>.</param>
    public DeveloperExceptionPageMiddleware(
        RequestDelegate next,
        IOptions<DeveloperExceptionPageOptions> options,
        ILoggerFactory loggerFactory,
        IWebHostEnvironment hostingEnvironment,
        DiagnosticSource diagnosticSource,
        IEnumerable<IDeveloperPageExceptionFilter> filters)
    {
        _innerMiddlewareImpl = new(
            next,
            options,
            loggerFactory,
            hostingEnvironment,
            diagnosticSource,
            filters,
            new DummyMeterFactory(),
            problemDetailsService: null);
    }

    /// <summary>
    /// Process an individual request.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task Invoke(HttpContext context)
        => _innerMiddlewareImpl.Invoke(context);
}
