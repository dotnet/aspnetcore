// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.BlazorPack;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure an <see cref="IServiceCollection"/> for components.
/// </summary>
public static class ComponentServiceCollectionExtensions
{
    /// <summary>
    /// Adds Server-Side Blazor services to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">A callback to configure <see cref="CircuitOptions"/>.</param>
    /// <returns>An <see cref="IServerSideBlazorBuilder"/> that can be used to further customize the configuration.</returns>
    [RequiresUnreferencedCode("Server-side Blazor does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/trimming")]
    public static IServerSideBlazorBuilder AddServerSideBlazor(this IServiceCollection services, Action<CircuitOptions>? configure = null)
    {
        var builder = new DefaultServerSideBlazorBuilder(services);

        services.AddDataProtection();

        services.TryAddScoped<ProtectedBrowserStorageJsonSerializerOptions>();
        services.TryAddScoped(static services => new ProtectedLocalStorage(
            services.GetRequiredService<IJSRuntime>(),
            services.GetRequiredService<IDataProtectionProvider>(),
            services.GetRequiredService<ProtectedBrowserStorageJsonSerializerOptions>().Options));
        services.TryAddScoped(static services => new ProtectedSessionStorage(
            services.GetRequiredService<IJSRuntime>(),
            services.GetRequiredService<IDataProtectionProvider>(),
            services.GetRequiredService<ProtectedBrowserStorageJsonSerializerOptions>().Options));

        // This call INTENTIONALLY uses the AddHubOptions on the SignalR builder, because it will merge
        // the global HubOptions before running the configure callback. We want to ensure that happens
        // once. Our AddHubOptions method doesn't do this.
        //
        // We need to restrict the set of protocols used by default to our specialized one. Users have
        // the chance to modify options further via the builder.
        //
        // Other than the options, the things exposed by the SignalR builder aren't very meaningful in
        // the Server-Side Blazor context and thus aren't exposed.
        services.AddSignalR().AddHubOptions<ComponentHub>(options =>
        {
            options.SupportedProtocols.Clear();
            options.SupportedProtocols.Add(BlazorPackHubProtocol.ProtocolName);
        });

        // Register the Blazor specific hub protocol
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, BlazorPackHubProtocol>());

        // Here we add a bunch of services that don't vary in any way based on the
        // user's configuration. So even if the user has multiple independent server-side
        // Components entrypoints, this lot is the same and repeated registrations are a no-op.

        services.TryAddSingleton<CircuitMetrics>();
        services.TryAddSingleton<ICircuitFactory, CircuitFactory>();
        services.TryAddSingleton<ICircuitHandleRegistry, CircuitHandleRegistry>();
        services.TryAddSingleton<RootTypeCache>();
        services.TryAddSingleton<ComponentParameterDeserializer>();
        services.TryAddSingleton<ComponentParametersTypeCache>();
        services.TryAddSingleton<CircuitIdFactory>();
        services.TryAddSingleton<ServerComponentSerializer>();
        services.TryAddScoped<IServerComponentDeserializer, ServerComponentDeserializer>();
        services.TryAddScoped<IErrorBoundaryLogger, RemoteErrorBoundaryLogger>();
        services.TryAddScoped<AntiforgeryStateProvider, DefaultAntiforgeryStateProvider>();

        services.TryAddScoped(s => s.GetRequiredService<ICircuitAccessor>().Circuit);
        services.TryAddScoped<ICircuitAccessor, DefaultCircuitAccessor>();
        services.TryAddSingleton<ISystemClock, SystemClock>();
        services.TryAddSingleton<CircuitRegistry>();
        services.TryAddSingleton<CircuitPersistenceManager>();

        // Register the circuit persistence provider conditionally based on HybridCache availability
        services.TryAddSingleton<ICircuitPersistenceProvider>(serviceProvider =>
        {
            var circuitOptions = serviceProvider.GetRequiredService<IOptions<CircuitOptions>>();
            if (circuitOptions.Value.HybridPersistenceCache is not null)
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ICircuitPersistenceProvider>>();
                return new HybridCacheCircuitPersistenceProvider(circuitOptions.Value.HybridPersistenceCache, logger, circuitOptions);
            }
            else
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ICircuitPersistenceProvider>>();
                var clock = serviceProvider.GetRequiredService<ISystemClock>();
                return new DefaultInMemoryCircuitPersistenceProvider(clock, logger, circuitOptions);
            }
        });

        // Register the configurator for HybridCache
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CircuitOptions>, DefaultHybridCache>());

        // Standard blazor hosting services implementations
        //
        // Register concrete Remote* services and select them when inside a circuit scope (HasCircuitContext == true),
        // while preserving SSR or user-registered scoped services (such as HttpNavigationManager) outside circuit scopes (#69938).
        services.TryAddScoped<RemoteNavigationManager>();
        services.TryAddScoped<RemoteJSRuntime>();
        services.TryAddScoped<RemoteNavigationInterception>();
        services.TryAddScoped<RemoteScrollToLocationHash>();

        RegisterCircuitScopedService<NavigationManager, RemoteNavigationManager>(services);
        RegisterCircuitScopedService<IJSRuntime, RemoteJSRuntime>(services);
        RegisterCircuitScopedService<INavigationInterception, RemoteNavigationInterception>(services);
        RegisterCircuitScopedService<IScrollToLocationHash, RemoteScrollToLocationHash>(services);
        services.TryAddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CircuitOptions>, CircuitOptionsJSInteropDetailedErrorsConfiguration>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CircuitOptions>, CircuitOptionsJavaScriptInitializersConfiguration>());

        services.TryAddScoped<CircuitActivitySource>();

        if (configure != null)
        {
            services.Configure(configure);
        }

        return builder;
    }

    private static void RegisterCircuitScopedService<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCircuitImplementation>(IServiceCollection services)
        where TService : class
        where TCircuitImplementation : class, TService
    {
        ServiceDescriptor? existingDescriptor = null;
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(TService))
            {
                existingDescriptor = services[i];
                break;
            }
        }
        if (existingDescriptor is null)
        {
            services.AddScoped<TService>(sp => sp.GetRequiredService<TCircuitImplementation>());
            return;
        }

        if (existingDescriptor.ImplementationType == typeof(TCircuitImplementation))
        {
            return;
        }

        services.AddScoped<TService>(sp =>
        {
            if (sp.GetService<ICircuitAccessor>() is { HasCircuitContext: true })
            {
                return sp.GetRequiredService<TCircuitImplementation>();
            }

            if (existingDescriptor.ImplementationFactory is { } factory)
            {
                return (TService)factory(sp);
            }

            if (existingDescriptor.ImplementationInstance is TService instance)
            {
                return instance;
            }

            if (existingDescriptor.ImplementationType is { } implType)
            {
                return (TService)ActivatorUtilities.GetServiceOrCreateInstance(sp, implType);
            }

            return sp.GetRequiredService<TCircuitImplementation>();
        });
    }

    private sealed class DefaultServerSideBlazorBuilder : IServerSideBlazorBuilder
    {
        public DefaultServerSideBlazorBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
