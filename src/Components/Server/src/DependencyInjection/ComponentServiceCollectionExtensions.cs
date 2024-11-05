// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.BlazorPack;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

        services.TryAddScoped<ProtectedLocalStorage>();
        services.TryAddScoped<ProtectedSessionStorage>();

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
        services.TryAddSingleton<ICircuitFactory, CircuitFactory>();
        services.TryAddSingleton<ICircuitHandleRegistry, CircuitHandleRegistry>();
        services.TryAddSingleton<RootComponentTypeCache>();
        services.TryAddSingleton<ComponentParameterDeserializer>();
        services.TryAddSingleton<ComponentParametersTypeCache>();
        services.TryAddSingleton<CircuitIdFactory>();
        services.TryAddScoped<IServerComponentDeserializer, ServerComponentDeserializer>();
        services.TryAddScoped<IErrorBoundaryLogger, RemoteErrorBoundaryLogger>();
        services.TryAddScoped<AntiforgeryStateProvider, DefaultAntiforgeryStateProvider>();

        services.TryAddScoped(s => s.GetRequiredService<ICircuitAccessor>().Circuit);
        services.TryAddScoped<ICircuitAccessor, DefaultCircuitAccessor>();

        services.TryAddSingleton<CircuitRegistry>();

        // Standard blazor hosting services implementations
        //
        // These intentionally replace the non-interactive versions included in MVC.
        services.AddScoped<NavigationManager, RemoteNavigationManager>();
        services.AddScoped<IJSRuntime, RemoteJSRuntime>();
        services.AddScoped<INavigationInterception, RemoteNavigationInterception>();
        services.AddScoped<IScrollToLocationHash, RemoteScrollToLocationHash>();
        services.TryAddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CircuitOptions>, CircuitOptionsJSInteropDetailedErrorsConfiguration>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CircuitOptions>, CircuitOptionsJavaScriptInitializersConfiguration>());

        if (configure != null)
        {
            services.Configure(configure);
        }

        return builder;
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
