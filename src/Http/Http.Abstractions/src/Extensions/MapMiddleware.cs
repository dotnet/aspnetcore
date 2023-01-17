// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.Extensions;

/// <summary>
/// Represents a middleware that maps a request path to a sub-request pipeline.
/// </summary>
public class MapMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MapOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="MapMiddleware"/>.
    /// </summary>
    /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
    /// <param name="options">The middleware options.</param>
    public MapMiddleware(RequestDelegate next, MapOptions options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Branch == null)
        {
            throw new ArgumentException("Branch not set on options.", nameof(options));
        }

        _next = next;
        _options = options;
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Path.StartsWithSegments(_options.PathMatch, out var matchedPath, out var remainingPath))
        {
            if (!_options.PreserveMatchedPathSegment)
            {
                return InvokeCore(context, matchedPath, remainingPath);
            }
            return _options.Branch!(context);
        }
        return _next(context);
    }

    private async Task InvokeCore(HttpContext context, PathString matchedPath, PathString remainingPath)
    {
        var path = context.Request.Path;
        var pathBase = context.Request.PathBase;

        // Update the path
        context.Request.PathBase = pathBase.Add(matchedPath);
        context.Request.Path = remainingPath;

        try
        {
            await _options.Branch!(context);
        }
        finally
        {
            context.Request.PathBase = pathBase;
            context.Request.Path = path;
        }
    }
}
