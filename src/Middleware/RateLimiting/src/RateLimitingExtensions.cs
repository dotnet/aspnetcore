// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Extension methods for the RateLimiting middleware.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Enables rate limiting for the application.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        return app.UseMiddleware<RateLimitingMiddleware>();
    }
}
