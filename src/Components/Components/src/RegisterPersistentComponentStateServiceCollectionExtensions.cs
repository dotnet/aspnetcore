// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Infrastructure APIs for registering services that persist state.
/// </summary>
public static class RegisterPersistentComponentStateServiceCollectionExtensions
{
    /// <summary>
    /// Saves <typeparamref name="TService"/> state when the application is persisting state and restores it at the appropriate time automatically.
    /// </summary>
    /// <remarks>
    /// Only public properties annotated with <see cref="SupplyParameterFromPersistentComponentStateAttribute"/> are persisted and restored.
    /// </remarks>
    /// <typeparam name="TService">The service type to register for persistence.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="componentRenderMode">The <see cref="IComponentRenderMode"/> to register the service for.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddPersistentServiceRegistration<[DynamicallyAccessedMembers(JsonSerialized)] TService>(
        IServiceCollection services,
        IComponentRenderMode componentRenderMode)
    {
        // This method does something very similar to what we do when we register root components, except in this case we are registering services.
        // We collect a list of all the registrations on during static rendering mode and push those registrations to interactive mode.
        // When the interactive mode starts, we retrieve the registry, and this triggers the process for restoring all the states for all the services.
        // The process for retrieving the services is the same as we do for root components.
        // We look for the assembly in the current list of loaded assemblies.
        // We look for the type inside the assembly.
        // We resolve the service from the DI container.
        // We loop through the properties in the type and try to restore the properties that have SupplyParameterFromPersistentComponentState on them.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPersistentServiceRegistration>(new PersistentServiceRegistration<TService>(componentRenderMode)));

        return services;
    }
}
