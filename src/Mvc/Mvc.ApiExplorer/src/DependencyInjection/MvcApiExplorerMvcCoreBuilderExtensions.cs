// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring ApiExplorer using an <see cref="IMvcCoreBuilder"/>.
/// </summary>
public static class MvcApiExplorerMvcCoreBuilderExtensions
{
    /// <summary>
    /// Configures <see cref="IMvcCoreBuilder"/> to use ApiExplorer.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddApiExplorer(this IMvcCoreBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddApiExplorerServices(builder.Services);
        return builder;
    }

    // Internal for testing.
    internal static void AddApiExplorerServices(IServiceCollection services)
    {
        services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, ApiDescriptionGroupCollectionProvider>();
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>());
    }
}
