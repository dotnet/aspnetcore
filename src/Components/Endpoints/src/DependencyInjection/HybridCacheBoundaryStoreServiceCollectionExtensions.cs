// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.Extensions.Caching.Hybrid;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring a <c>CacheBoundary</c> store that uses
/// <see cref="HybridCache"/>.
/// </summary>
public static class HybridCacheBoundaryStoreServiceCollectionExtensions
{
    /// <summary>
    /// Selects a <c>CacheBoundary</c> store that uses <see cref="HybridCache"/> instead of the
    /// default store that uses <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>.
    /// <see cref="HybridCache"/> must be registered separately. The selection is applied additively and is independent of the
    /// order in which services are registered.
    /// </summary>
    /// <param name="builder">The <see cref="IRazorComponentsBuilder"/>.</param>
    /// <returns>The same <see cref="IRazorComponentsBuilder"/> for chaining.</returns>
    public static IRazorComponentsBuilder AddHybridCacheBoundaryStore(this IRazorComponentsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Configure<CacheBoundaryStoreOptions>(static options => options.UseHybridCache = true);
        return builder;
    }
}
