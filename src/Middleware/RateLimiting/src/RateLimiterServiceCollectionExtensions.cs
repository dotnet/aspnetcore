// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the RateLimiting middleware.
/// </summary>
public static class RateLimiterServiceCollectionExtensions
{
    /// <summary>
    /// Add rate limiting services and configure the related options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="RateLimiterOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddRateLimiter(this IServiceCollection services, Action<RateLimiterOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddRateLimiter();
        services.Configure(configureOptions);
        return services;
    }

    /// <summary>
    /// Add rate limiting services and configure the related options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns></returns>
    public static IServiceCollection AddRateLimiter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMetrics();
        services.AddSingleton<RateLimitingMetrics>();
        return services;
    }
}
