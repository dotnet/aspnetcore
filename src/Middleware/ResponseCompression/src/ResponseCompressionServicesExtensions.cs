// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the ResponseCompression middleware.
/// </summary>
public static class ResponseCompressionServicesExtensions
{
    /// <summary>
    /// Add response compression services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddResponseCompression(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IResponseCompressionProvider, ResponseCompressionProvider>();
        return services;
    }

    /// <summary>
    /// Add response compression services and configure the related options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="ResponseCompressionOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddResponseCompression(this IServiceCollection services, Action<ResponseCompressionOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.TryAddSingleton<IResponseCompressionProvider, ResponseCompressionProvider>();
        return services;
    }
}
