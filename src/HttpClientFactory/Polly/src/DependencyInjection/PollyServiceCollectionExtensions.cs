// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Polly.Registry;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides convenience extension methods to register <see cref="IPolicyRegistry{String}"/> and
/// <see cref="IReadOnlyPolicyRegistry{String}"/> in the service collection.
/// </summary>
public static class PollyServiceCollectionExtensions
{
    /// <summary>
    /// Registers an empty <see cref="PolicyRegistry"/> in the service collection with service types
    /// <see cref="IPolicyRegistry{String}"/>, <see cref="IReadOnlyPolicyRegistry{String}"/>, and
    /// <see cref="IConcurrentPolicyRegistry{String}"/> if the service types haven't already been registered
    /// and returns the newly created registry.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The newly created <see cref="IPolicyRegistry{String}"/>.</returns>
    public static IPolicyRegistry<string> AddPolicyRegistry(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Create an empty registry, register and return it as an instance. This is the best way to get a
        // single instance registered using all the interfaces.
        var registry = new PolicyRegistry();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPolicyRegistry<string>>(registry));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReadOnlyPolicyRegistry<string>>(registry));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConcurrentPolicyRegistry<string>>(registry));

        return registry;
    }

    /// <summary>
    /// Registers the provided <see cref="IPolicyRegistry{String}"/> in the service collection with service types
    /// <see cref="IPolicyRegistry{String}"/>, <see cref="IReadOnlyPolicyRegistry{String}"/>, and
    /// <see cref="IConcurrentPolicyRegistry{String}"/> if the service types haven't already been registered
    /// and returns the newly created registry.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="registry">The <see cref="IPolicyRegistry{String}"/>.</param>
    /// <returns>The provided <see cref="IPolicyRegistry{String}"/>.</returns>
    public static IPolicyRegistry<string> AddPolicyRegistry(this IServiceCollection services, IPolicyRegistry<string> registry)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPolicyRegistry<string>>(registry));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReadOnlyPolicyRegistry<string>>(registry));

        if (registry is IConcurrentPolicyRegistry<string> concurrentRegistry)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConcurrentPolicyRegistry<string>>(concurrentRegistry));
        }

        return registry;
    }

    /// <summary>
    /// Registers an empty <see cref="PolicyRegistry"/> in the service collection with service types
    /// <see cref="IPolicyRegistry{String}"/>, <see cref="IReadOnlyPolicyRegistry{String}"/>, and
    /// <see cref="IConcurrentPolicyRegistry{String}"/> if the service types haven't already been registered
    /// and uses the specified delegate to configure it.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configureRegistry">A delegate that is used to configure an <see cref="IPolicyRegistry{String}"/>.</param>
    /// <returns>The provided <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddPolicyRegistry(this IServiceCollection services, Action<IServiceProvider, IPolicyRegistry<string>> configureRegistry)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureRegistry == null)
        {
            throw new ArgumentNullException(nameof(configureRegistry));
        }

        // Create an empty registry, configure it and register it as an instance.
        // This is the best way to get a single instance registered using all the interfaces.
        services.TryAddSingleton(serviceProvider =>
        {
            var registry = new PolicyRegistry();

            configureRegistry(serviceProvider, registry);

            return registry;
        });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPolicyRegistry<string>>(serviceProvider => serviceProvider.GetRequiredService<PolicyRegistry>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReadOnlyPolicyRegistry<string>>(serviceProvider => serviceProvider.GetRequiredService<PolicyRegistry>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConcurrentPolicyRegistry<string>>(serviceProvider => serviceProvider.GetRequiredService<PolicyRegistry>()));

        return services;
    }
}
