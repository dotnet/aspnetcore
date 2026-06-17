// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.OutputCaching.StackExchangeRedis;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up Redis output cache related services in an <see cref="IServiceCollection" />.
/// </summary>
public static class StackExchangeRedisOutputCacheServiceCollectionExtensions
{
    /// <summary>
    /// Adds Redis output caching services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{RedisOutputCacheOptions}"/> to configure the provided
    /// <see cref="RedisOutputCacheOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddStackExchangeRedisOutputCache(this IServiceCollection services, Action<RedisOutputCacheOptions> setupAction)
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);
        ArgumentNullThrowHelper.ThrowIfNull(setupAction);

        services.AddOptions();

        services.Configure(setupAction);
        // replace here (Add vs TryAdd) is intentional and part of test conditions
        // long-form name qualification is because of the #if conditional; we'd need a matching #if around
        // a using directive, which is messy
        services.AddSingleton<AspNetCore.OutputCaching.IOutputCacheStore, RedisOutputCacheStoreImpl>();

        return services;
    }
}
