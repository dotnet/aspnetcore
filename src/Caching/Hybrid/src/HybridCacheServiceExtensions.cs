// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Configuration extension methods for <see cref="HybridCache"/>.
/// </summary>
public static class HybridCacheServiceExtensions
{
    /// <summary>
    /// Adds support for multi-tier caching services.
    /// </summary>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> system.</returns>
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services, Action<HybridCacheOptions> setupAction)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(setupAction);
#else
        _ = setupAction ?? throw new ArgumentNullException(nameof(setupAction));
#endif
        AddHybridCache(services);
        services.Configure(setupAction);
        return new HybridCacheBuilder(services);
    }

    /// <summary>
    /// Adds support for multi-tier caching services.
    /// </summary>
    /// <returns>A builder instance that allows further configuration of the <see cref="HybridCache"/> system.</returns>
    public static IHybridCacheBuilder AddHybridCache(this IServiceCollection services)
    {
#if NET7_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(services);
#else
        _ = services ?? throw new ArgumentNullException(nameof(services));
#endif

#if NET8_0_OR_GREATER
        services.TryAddSingleton(TimeProvider.System);
#else
        services.TryAddSingleton<ISystemClock, SystemClock>();
#endif
        services.AddOptions();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache(); // we need a backend; use in-proc by default
        services.AddSingleton<IHybridCacheSerializerFactory, DefaultJsonSerializerFactory>();
        services.AddSingleton<IHybridCacheSerializer<string>>(InbuiltTypeSerializer.Instance);
        services.AddSingleton<IHybridCacheSerializer<byte[]>>(InbuiltTypeSerializer.Instance);
        services.AddSingleton<HybridCache, DefaultHybridCache>();
        return new HybridCacheBuilder(services);
    }
}
