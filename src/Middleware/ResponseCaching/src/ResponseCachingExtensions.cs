// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.ResponseCaching;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding the <see cref="ResponseCachingMiddleware"/> to an application.
/// </summary>
public static class ResponseCachingExtensions
{
    /// <summary>
    /// Adds the <see cref="ResponseCachingMiddleware"/> for caching HTTP responses.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    public static IApplicationBuilder UseResponseCaching(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<ResponseCachingMiddleware>();
    }
}
