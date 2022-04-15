// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Extension methods for the RateLimiting middleware.
/// </summary>
public static class RateLimitingServicesExtensions
{
    /// <summary>
    /// Configures rate limiting services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="RateLimitingOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection ConfigureRateLimiting(this IServiceCollection services, Action<RateLimitingOptions> configureOptions)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        if (configureOptions == null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        services.Configure(configureOptions);
        return services;
    }
}
