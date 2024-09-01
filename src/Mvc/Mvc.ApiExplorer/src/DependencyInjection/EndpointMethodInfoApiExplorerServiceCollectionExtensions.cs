// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring ApiExplorer using <see cref="Endpoint.Metadata"/>.
/// </summary>
public static class EndpointMetadataApiExplorerServiceCollectionExtensions
{
    /// <summary>
    /// Configures ApiExplorer using <see cref="Endpoint.Metadata"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    public static IServiceCollection AddEndpointsApiExplorer(this IServiceCollection services)
    {
        // Try to add default services in case MVC services aren't added.
        services.TryAddSingleton<IActionDescriptorCollectionProvider, DefaultActionDescriptorCollectionProvider>();
        services.TryAddSingleton<IApiDescriptionGroupCollectionProvider>(sp => new ApiDescriptionGroupCollectionProvider(
            sp.GetRequiredService<IActionDescriptorCollectionProvider>(),
            sp.GetServices<IApiDescriptionProvider>(),
            sp.GetRequiredService<ILoggerFactory>()));

        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IApiDescriptionProvider, EndpointMetadataApiDescriptionProvider>());

        return services;
    }
}
