// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OutputCaching;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding output caching to an application.
/// </summary>
public static class OutputCacheApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the output caching middleware for caching HTTP responses.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    public static IApplicationBuilder UseOutputCache(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<OutputCacheMiddleware>();
    }
}
