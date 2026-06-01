// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring an <see cref="HybridCache"/>-backed
/// <c>CacheBoundary</c> store.
/// </summary>
public static class HybridCacheBoundaryStoreServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default in-memory cache boundary store with one backed by <see cref="HybridCache"/>.
    /// <see cref="HybridCache"/> must be registered separately (typically via <c>AddHybridCache</c>).
    /// </summary>
    /// <param name="builder">The <see cref="IRazorComponentsBuilder"/>.</param>
    /// <returns>The same <see cref="IRazorComponentsBuilder"/> for chaining.</returns>
    public static IRazorComponentsBuilder AddHybridCacheBoundaryStore(this IRazorComponentsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Replace(ServiceDescriptor.Singleton<ICacheBoundaryStore, HybridCacheBoundaryStore>());
        return builder;
    }
}
