// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.Extensions;

/// <summary>
/// Represents a middleware that extracts the specified path base from request path and postpend it to the request path base.
/// </summary>
public class UsePathBaseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PathString _pathBase;

    /// <summary>
    /// Creates a new instance of <see cref="UsePathBaseMiddleware"/>.
    /// </summary>
    /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
    /// <param name="pathBase">The path base to extract.</param>
    public UsePathBaseMiddleware(RequestDelegate next, PathString pathBase)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!pathBase.HasValue)
        {
            throw new ArgumentException($"{nameof(pathBase)} cannot be null or empty.");
        }

        _next = next;
        _pathBase = pathBase;
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Path.StartsWithSegments(_pathBase, out var matchedPath, out var remainingPath))
        {
            return InvokeCore(context, matchedPath, remainingPath);
        }
        return _next(context);
    }

    private async Task InvokeCore(HttpContext context, PathString matchedPath, PathString remainingPath)
    {
        var originalPath = context.Request.Path;
        var originalPathBase = context.Request.PathBase;
        context.Request.Path = remainingPath;
        context.Request.PathBase = originalPathBase.Add(matchedPath);

        try
        {
            await _next(context);
        }
        finally
        {
            context.Request.Path = originalPath;
            context.Request.PathBase = originalPathBase;
        }
    }
}
