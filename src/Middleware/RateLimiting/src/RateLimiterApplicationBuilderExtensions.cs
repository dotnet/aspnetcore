// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Resources = Microsoft.AspNetCore.RateLimiting.Resources;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the RateLimiting middleware.
/// </summary>
public static class RateLimiterApplicationBuilderExtensions
{
    /// <summary>
    /// Enables rate limiting for the application.
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRateLimiter(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        VerifyServicesAreRegistered(app);

        return app.UseMiddleware<RateLimitingMiddleware>();
    }

    /// <summary>
    /// Enables rate limiting for the application.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRateLimiter(this IApplicationBuilder app, RateLimiterOptions options)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(options);

        VerifyServicesAreRegistered(app);

        return app.UseMiddleware<RateLimitingMiddleware>(Options.Create(options));
    }

    private static void VerifyServicesAreRegistered(IApplicationBuilder app)
    {
        var serviceProviderIsService = app.ApplicationServices.GetService<IServiceProviderIsService>();
        if (serviceProviderIsService != null && !serviceProviderIsService.IsService(typeof(RateLimitingMetrics)))
        {
            throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                nameof(IServiceCollection),
                nameof(RateLimiterServiceCollectionExtensions.AddRateLimiter)));
        }
    }
}
