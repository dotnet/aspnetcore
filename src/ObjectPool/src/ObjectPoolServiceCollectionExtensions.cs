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
public static class ObjectPoolServiceCollectionExtensions
{
    /// <summary>
    /// Adds an <see cref="ObjectPool{T}"/> and lets DI return scoped instances of T.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configureOptions">The action used to configure the options of the pool.</param>
    /// <typeparam name="TDefinition">The type of objects to pool.</typeparam>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The default capacity is 1024.
    /// The pooled type instances are obtainable the same way like any other type instances from the DI container.
    /// </remarks>
    public static IServiceCollection AddPooled<TDefinition>(this IServiceCollection services, Action<PoolOptions>? configureOptions = null)
        where TDefinition : class
    {
        return services.AddPooledInternal<TDefinition, TDefinition>(configureOptions);
    }

    /// <summary>
    /// Adds an <see cref="ObjectPool{T}"/> and let DI return scoped instances of T.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configureOptions">Configuration of the pool.</param>
    /// <typeparam name="TDefinition">The type of objects to pool.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <returns>Provided service collection.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The default capacity is 1024.
    /// The pooled type instances are obtainable the same way like any other type instances from the DI container.
    /// </remarks>
    public static IServiceCollection AddPooled<TDefinition, TImplementation>(this IServiceCollection services, Action<PoolOptions>? configureOptions = null)
        where TDefinition : class
        where TImplementation : class, TDefinition
    {
        return services.AddPooledInternal<TDefinition, TImplementation>(configureOptions);
    }

    /// <summary>
    /// Registers an action used to configure the <see cref="PoolOptions"/> of a typed pool.
    /// </summary>
    /// <typeparam name="TDefinition">The type of objects to pool.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection ConfigurePools<TDefinition>(this IServiceCollection services, Action<PoolOptions> configureOptions)
        where TDefinition : class
    {
        return services.Configure<PoolOptions>(typeof(TDefinition).FullName, configureOptions);
    }

    /// <summary>
    /// Registers an action used to configure the <see cref="PoolOptions"/> for all pools.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureOptions">The action used to configure the options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection ConfigurePools(this IServiceCollection services, Action<PoolOptions> configureOptions)
    {
        return services.Configure<PoolOptions>(configureOptions);
    }

    private static IServiceCollection AddPooledInternal<TDefinition, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceCollection services, Action<PoolOptions>? configureOptions)
        where TDefinition : class
        where TImplementation : class, TDefinition
    {
        // Register a PoolOption instance specific to the type, even if there is no specific configuration action
        // as this will be resolved when the pool is initialized.

        services.ConfigurePools<TDefinition>(configureOptions ?? (_ => { }));

        return services
            .AddSingleton<ObjectPool<TDefinition>>(provider =>
            {
                var options = provider.GetRequiredService<IOptionsMonitor<PoolOptions>>().Get(typeof(TDefinition).FullName);
                return new DefaultObjectPool<TDefinition>(new DependencyInjectionPooledObjectPolicy<TDefinition, TImplementation>(provider), options.Capacity);
            });
    }
}
