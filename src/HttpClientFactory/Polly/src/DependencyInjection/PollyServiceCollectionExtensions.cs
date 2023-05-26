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

        // Get existing registry or an empty instance
        var registry = services.BuildServiceProvider().GetService<IPolicyRegistry<string>>();
        if (registry == null)
        {
            registry = new PolicyRegistry();
        }

        // Try to register for the missing interfaces
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPolicyRegistry<string>>(registry));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReadOnlyPolicyRegistry<string>>(registry));

        if (registry is IConcurrentPolicyRegistry<string> concurrentRegistry)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConcurrentPolicyRegistry<string>>(concurrentRegistry));
        }

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

        // Get existing registry or use the given instance
        var existingRegistry = services.BuildServiceProvider().GetService<IPolicyRegistry<string>>();
        if (existingRegistry != null)
        {
            // Move the new policies to the existing registry
            foreach (var keyValuePair in registry)
            {
                existingRegistry.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }
        else
        {
            existingRegistry = registry;
        }

        // Try to register for the missing interfaces
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPolicyRegistry<string>>(existingRegistry));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReadOnlyPolicyRegistry<string>>(existingRegistry));

        if (existingRegistry is IConcurrentPolicyRegistry<string> concurrentRegistry)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConcurrentPolicyRegistry<string>>(concurrentRegistry));
        }

        return existingRegistry;
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

        // Build the service provider to be able to configure the registry.
        // Building the services at this point may throw an exception
        // if the delegate has required dependencies that were not yet registered,
        // but it's necessary to not lose configurations when this method is invoked multiple times
        var serviceProvider = services.BuildServiceProvider();

        // Get existing registry or an empty instance
        var registry = serviceProvider.GetService<IPolicyRegistry<string>>();
        if (registry == null)
        {
            registry = new PolicyRegistry();
        }

        configureRegistry(serviceProvider, registry);

        // Try to register for the missing interfaces
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPolicyRegistry<string>>(registry));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReadOnlyPolicyRegistry<string>>(registry));

        if (registry is IConcurrentPolicyRegistry<string> concurrentRegistry)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConcurrentPolicyRegistry<string>>(concurrentRegistry));
        }

        return services;
    }
}