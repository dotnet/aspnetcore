// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding directory browser services.
/// </summary>
public static class DirectoryBrowserServiceExtensions
{
    /// <summary>
    /// Adds directory browser middleware services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddDirectoryBrowser(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddWebEncoders();

        return services;
    }
}
