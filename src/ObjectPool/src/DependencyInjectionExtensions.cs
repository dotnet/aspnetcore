// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding <see cref="ObjectPool{T}"/> to DI container.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds an <see cref="ObjectPool{T}"/> and lets DI return scoped instances of T.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">The action used to configure the options of the pool.</param>
    /// <typeparam name="TDefinition">The type of objects to pool.</typeparam>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The default capacity is 1024.
    /// The pooled type instances are obtainable the same way like any other type instances from the DI container.
    /// </remarks>
    public static IServiceCollection AddPool<TDefinition>(this IServiceCollection services, Action<PoolOptions>? configure = null)
        where TDefinition : class
    {
        return services.AddPoolInternal<TDefinition, TDefinition>(configure);
    }

    /// <summary>
    /// Adds an <see cref="ObjectPool{T}"/> and let DI return scoped instances of T.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">Configuration of the pool.</param>
    /// <typeparam name="TDefinition">The type of objects to pool.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The default capacity is 1024.
    /// The pooled type instances are obtainable the same way like any other type instances from the DI container.
    /// </remarks>
    public static IServiceCollection AddPool<TDefinition, TImplementation>(this IServiceCollection services, Action<PoolOptions>? configure = null)
        where TDefinition : class
        where TImplementation : class, TDefinition
    {
        return services.AddPoolInternal<TDefinition, TImplementation>(configure);
    }

    private static IServiceCollection AddPoolInternal<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceCollection services, Action<PoolOptions>? configure)
        where TService : class
        where TImplementation : class, TService
    {
        return services
            .Configure(typeof(TService).FullName, configure ?? (_ => { }))
            .AddSingleton<ObjectPool<TService>>(provider =>
            {
                var options = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>().Get(typeof(TService).FullName);
                return new DefaultObjectPool<TService>(new DependencyInjectionPooledObjectPolicy<TService, TImplementation>(provider), options.Capacity);
            });
    }
}
