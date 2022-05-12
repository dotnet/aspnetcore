// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Extension methods for the RateLimiting middleware.
/// </summary>
public static class RateLimitingApplicationBuilderExtensions
{
    /// <summary>
    /// Enables rate limiting for the application.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRateLimiter(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<RateLimitingMiddleware>();
    }

    /// <summary>
    /// Enables rate limiting for the application.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRateLimiter(this IApplicationBuilder app, Action<RateLimiterOptions> options)
    {
        ArgumentNullException.ThrowIfNull(app, nameof(app));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var rateLimiterOptions = new RateLimiterOptions();
        options.Invoke(rateLimiterOptions);

        return app.UseMiddleware<RateLimitingMiddleware>(Options.Create(rateLimiterOptions));
    }
}
