// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Caching.Distributed;

public static class HybridCacheServiceExtensions
{
    public static IServiceCollection AddHybridCache(this IServiceCollection services, Action<HybridCacheOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(setupAction);
        AddHybridCache(services);
        services.Configure(setupAction);
        return services;
    }

    public static IServiceCollection AddHybridCache(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache(); // we need a backend; use in-proc by default
        services.AddSingleton<IHybridCacheSerializerFactory, DefaultJsonSerializerFactory>();
        services.AddSingleton<IHybridCacheSerializer<string>>(InbuiltTypeSerializer.Instance);
        services.AddSingleton<IHybridCacheSerializer<byte[]>>(InbuiltTypeSerializer.Instance);
        services.AddSingleton<HybridCache, DefaultHybridCache>();
        return services;
    }
}
