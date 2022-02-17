// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.RequestDecompression;

/// <summary>
/// Extension methods for the RequestDecompression middleware.
/// </summary>
public static class RequestDecompressionServiceExtensions
{
    /// <summary>
    /// Add request decompression services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRequestDecompression(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton<IRequestDecompressionProvider, RequestDecompressionProvider>();
        return services;
    }

    /// <summary>
    /// Add request decompression services and configure the related options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="RequestDecompressionOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRequestDecompression(this IServiceCollection services, Action<RequestDecompressionOptions> configureOptions)
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
        services.TryAddSingleton<IRequestDecompressionProvider, RequestDecompressionProvider>();
        return services;
    }
}
