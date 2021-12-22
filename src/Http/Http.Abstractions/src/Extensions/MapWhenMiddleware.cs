// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.Extensions;

/// <summary>
/// Represents a middleware that runs a sub-request pipeline when a given predicate is matched.
/// </summary>
public class MapWhenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly MapWhenOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="MapWhenMiddleware"/>.
    /// </summary>
    /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
    /// <param name="options">The middleware options.</param>
    public MapWhenMiddleware(RequestDelegate next, MapWhenOptions options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Predicate == null)
        {
            throw new ArgumentException("Predicate not set on options.", nameof(options));
        }

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

        if (_options.Predicate!(context))
        {
            return _options.Branch!(context);
        }
        return _next(context);
    }
}
