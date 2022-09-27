// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.MiddlewareAnalysis;

/// <summary>
/// Middleware that is inserted before and after each other middleware in the pipeline by <see cref="AnalysisBuilder"/>
/// to log to a <see cref="DiagnosticSource"/> when other middleware starts, finishes and throws.
/// </summary>
public class AnalysisMiddleware
{
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly RequestDelegate _next;
    private readonly DiagnosticSource _diagnostics;
    private readonly string _middlewareName;

    /// <summary>
    /// Initializes a new instance of <see cref="AnalysisMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="diagnosticSource">The <see cref="DiagnosticSource"/> to log when other middleware starts, finishes and throws.</param>
    /// <param name="middlewareName">
    /// The name of the next middleware in the pipeline. This name is typically retrieved from <see cref="Builder.IApplicationBuilder.Properties"/>
    /// using the "analysis.NextMiddlewareName" key.
    /// </param>
    public AnalysisMiddleware(RequestDelegate next, DiagnosticSource diagnosticSource, string middlewareName)
    {
        _next = next;
        _diagnostics = diagnosticSource;
        if (string.IsNullOrEmpty(middlewareName))
        {
            middlewareName = next.Target!.GetType().FullName!;
        }
        _middlewareName = middlewareName;
    }

    /// <summary>
    /// Executes the middleware that logs to a <see cref="DiagnosticSource"/> when the next middleware starts, finishes and throws.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    public async Task Invoke(HttpContext httpContext)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        if (_diagnostics.IsEnabled("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting"))
        {
            _diagnostics.Write(
                "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting",
                new
                {
                    name = _middlewareName,
                    httpContext = httpContext,
                    instanceId = _instanceId,
                    timestamp = startTimestamp,
                });
        }

        try
        {
            await _next(httpContext);

            if (_diagnostics.IsEnabled("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareFinished"))
            {
                var currentTimestamp = Stopwatch.GetTimestamp();
                _diagnostics.Write(
                    "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareFinished",
                    new
                    {
                        name = _middlewareName,
                        httpContext = httpContext,
                        instanceId = _instanceId,
                        timestamp = currentTimestamp,
                        duration = currentTimestamp - startTimestamp,
                    });
            }
        }
        catch (Exception ex)
        {
            if (_diagnostics.IsEnabled("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException"))
            {
                var currentTimestamp = Stopwatch.GetTimestamp();
                _diagnostics.Write(
                    "Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException",
                    new
                    {
                        name = _middlewareName,
                        httpContext = httpContext,
                        instanceId = _instanceId,
                        timestamp = currentTimestamp,
                        duration = currentTimestamp - startTimestamp,
                        exception = ex,
                    });
            }
            throw;
        }
    }
}
