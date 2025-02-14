// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Resources = Microsoft.AspNetCore.HttpLogging.Resources;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the HttpLogging middleware.
/// </summary>
public static class HttpLoggingBuilderExtensions
{
    /// <summary>
    /// Adds a middleware that can log HTTP requests and responses.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseHttpLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        VerifyHttpLoggingServicesAreRegistered(app);

        app.UseMiddleware<HttpLoggingMiddleware>();
        return app;
    }

    /// <summary>
    /// Adds a middleware that can log HTTP requests and responses for server logs in W3C format.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
    public static IApplicationBuilder UseW3CLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        VerifyW3CLoggingServicesAreRegistered(app);

        app.UseMiddleware<W3CLoggingMiddleware>();
        return app;
    }

    private static void VerifyHttpLoggingServicesAreRegistered(IApplicationBuilder app)
    {
        var serviceProviderIsService = app.ApplicationServices.GetService<IServiceProviderIsService>();
        if (serviceProviderIsService != null && (!serviceProviderIsService.IsService(typeof(ObjectPool<HttpLoggingInterceptorContext>)) ||
            !serviceProviderIsService.IsService(typeof(TimeProvider))))
        {
            throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                nameof(IServiceCollection),
                nameof(HttpLoggingServicesExtensions.AddHttpLogging)));
        }
    }

    private static void VerifyW3CLoggingServicesAreRegistered(IApplicationBuilder app)
    {
        var serviceProviderIsService = app.ApplicationServices.GetService<IServiceProviderIsService>();
        if (serviceProviderIsService != null && (!serviceProviderIsService.IsService(typeof(W3CLoggerProcessor)) ||
          !serviceProviderIsService.IsService(typeof(W3CLogger))))
        {
            throw new InvalidOperationException(Resources.FormatUnableToFindServices(
                nameof(IServiceCollection),
                nameof(HttpLoggingServicesExtensions.AddW3CLogging)));
        }
    }
}
