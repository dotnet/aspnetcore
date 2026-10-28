// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Infrastructure APIs for registering diagnostic metrics.
/// </summary>
public static class ComponentsMetricsServiceCollectionExtensions
{
    /// <summary>
    /// Registers component rendering metrics
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddComponentsMetrics(
        IServiceCollection services)
    {
        // do not register IConfigureOptions<StartupValidatorOptions> multiple times
        if (!IsMeterFactoryRegistered(services))
        {
            services.AddMetrics();
        }
        services.TryAddSingleton<ComponentsMetrics>();

        return services;
    }

    /// <summary>
    /// Registers component rendering traces
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddComponentsTracing(
        IServiceCollection services)
    {
        services.TryAddScoped<ComponentsActivitySource>();
        AddComponentsActivityStatePersistence(services);

        return services;
    }

    internal static void AddComponentsActivityStatePersistence(IServiceCollection services)
    {
        // The persistence manager owns the state and can resolve these wrappers from the WASM root provider.
        services.TryAddTransient<ServerComponentsActivityState>();
        services.TryAddTransient<WebAssemblyComponentsActivityState>();
        services.TryAddSingleton<RegisteredPersistentServiceRegistrationCollection>();
        services.TryAddKeyedSingleton<IComponentRenderMode, UnsupportedComponentsActivityStateRenderMode>(
            "Microsoft.AspNetCore.Components.ActivityState.Server");
        services.TryAddKeyedSingleton<IComponentRenderMode, UnsupportedComponentsActivityStateRenderMode>(
            "Microsoft.AspNetCore.Components.ActivityState.WebAssembly");
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersistentServiceRegistration, ServerComponentsActivityStateRegistration>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPersistentServiceRegistration, WebAssemblyComponentsActivityStateRegistration>());
    }

    private sealed class UnsupportedComponentsActivityStateRenderMode : IComponentRenderMode;

    private sealed class ServerComponentsActivityStateRegistration(IServiceProvider serviceProvider) : IPersistentServiceRegistration
    {
        private readonly PersistentServiceRegistration<ServerComponentsActivityState> _registration = new(
            serviceProvider.GetRequiredKeyedService<IComponentRenderMode>("Microsoft.AspNetCore.Components.ActivityState.Server"));

        public string Assembly => _registration.Assembly;
        public string FullTypeName => _registration.FullTypeName;
        public IComponentRenderMode? GetRenderModeOrDefault() => _registration.GetRenderModeOrDefault();

        [DynamicDependency(JsonSerialized, typeof(ServerComponentsActivityState))]
        public Type? GetResolvedTypeOrNull() => _registration.GetResolvedTypeOrNull();
    }

    private sealed class WebAssemblyComponentsActivityStateRegistration(IServiceProvider serviceProvider) : IPersistentServiceRegistration
    {
        private readonly PersistentServiceRegistration<WebAssemblyComponentsActivityState> _registration = new(
            serviceProvider.GetRequiredKeyedService<IComponentRenderMode>("Microsoft.AspNetCore.Components.ActivityState.WebAssembly"));

        public string Assembly => _registration.Assembly;
        public string FullTypeName => _registration.FullTypeName;
        public IComponentRenderMode? GetRenderModeOrDefault() => _registration.GetRenderModeOrDefault();

        [DynamicDependency(JsonSerialized, typeof(WebAssemblyComponentsActivityState))]
        public Type? GetResolvedTypeOrNull() => _registration.GetResolvedTypeOrNull();
    }

    private static bool IsMeterFactoryRegistered(IServiceCollection services)
    {
        foreach (var service in services)
        {
            if (service.ServiceType == typeof(IMeterFactory))
            {
                return true;
            }
        }
        return false;
    }
}
