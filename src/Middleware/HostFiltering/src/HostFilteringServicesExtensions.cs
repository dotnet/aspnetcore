// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the host filtering middleware.
/// </summary>
public static class HostFilteringServicesExtensions
{
    /// <summary>
    /// Adds services and options for the host filtering middleware.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> for adding services.</param>
    /// <param name="configureOptions">A delegate to configure the <see cref="HostFilteringOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddHostFiltering(this IServiceCollection services, Action<HostFilteringOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        return services;
    }
}
