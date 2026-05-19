// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.RazorViews;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// This middleware provides a default web page for new applications.
/// </summary>
public class WelcomePageMiddleware
{
    private readonly RequestDelegate _next;
    private readonly WelcomePageOptions _options;

    /// <summary>
    /// Creates a default web page for new applications.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="options"></param>
    public WelcomePageMiddleware(RequestDelegate next, IOptions<WelcomePageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);

        _next = next;
        _options = options.Value;
    }

    /// <summary>
    /// Process an individual request.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns></returns>
    public Task Invoke(HttpContext context)
    {
        HttpRequest request = context.Request;
        if (!_options.Path.HasValue || _options.Path == request.Path)
        {
            // Dynamically generated for LOC.
            var welcomePage = new WelcomePage();
            return welcomePage.ExecuteAsync(context);
        }

        return _next(context);
    }
}
