// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Http2SampleApp;

// You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
public class TimingMiddleware
{
    private readonly RequestDelegate _next;

    public TimingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        if (httpContext.Response.SupportsTrailers())
        {
            httpContext.Response.DeclareTrailer("Server-Timing");

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            await _next(httpContext);

            stopWatch.Stop();
            // Not yet supported in any browser dev tools
            httpContext.Response.AppendTrailer("Server-Timing", $"app;dur={stopWatch.ElapsedMilliseconds}.0");
        }
        else
        {
            // Works in chrome
            // httpContext.Response.Headers.Append("Server-Timing", $"app;dur=25.0");
            await _next(httpContext);
        }
    }
}

// Extension method used to add the middleware to the HTTP request pipeline.
public static class TimingMiddlewareExtensions
{
    public static IApplicationBuilder UseTimingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TimingMiddleware>();
    }
}
