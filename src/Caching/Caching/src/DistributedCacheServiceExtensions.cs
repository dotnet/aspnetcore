// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.Caching.Distributed;

public static class DistributedCacheServiceExtensions
{
    public static IServiceCollection AddTypedDistributedCache(this IServiceCollection services, Action<TypedDistributedCacheOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(setupAction);
        AddTypedDistributedCache(services);
        services.Configure(setupAction);
        return services;
    }
    public static IServiceCollection AddTypedDistributedCache(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions();
        services.AddDistributedMemoryCache(); // we need a backend; use in-proc by default
        services.TryAddSingleton(typeof(ICacheSerializer<string>), typeof(StringSerializer));
        services.TryAddSingleton(typeof(ICacheSerializer<>), typeof(DefaultJsonSerializer<>));
        services.AddSingleton(typeof(IDistributedCache<>), typeof(DistributedCache<>));
        return services;
    }
}

public sealed class TypedDistributedCacheOptions
{
    // TBD
}

