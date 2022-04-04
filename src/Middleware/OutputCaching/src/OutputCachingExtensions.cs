// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OutputCaching;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding the <see cref="OutputCachingMiddleware"/> to an application.
/// </summary>
public static class OutputCachingExtensions
{
    /// <summary>
    /// Adds the <see cref="OutputCachingMiddleware"/> for caching HTTP responses.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    public static IApplicationBuilder UseOutputCaching(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));

        return app.UseMiddleware<OutputCachingMiddleware>();
    }
}
