// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.BlazorPack;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure an <see cref="IServiceCollection"/> for components.
/// </summary>
public static class RazorComponentsBuilderExtensions
{
    /// <summary>
    /// Adds services to support rendering interactive server components in a razor components
    /// application.
    /// </summary>
    /// <param name="builder">The <see cref="IRazorComponentsBuilder"/>.</param>
    /// <param name="configure">A callback to configure <see cref="CircuitOptions"/>.</param>
    /// <returns>An <see cref="IRazorComponentsBuilder"/> that can be used to further customize the configuration.</returns>
    [RequiresUnreferencedCode("Server-side Blazor does not currently support native AOT.", Url = "https://aka.ms/aspnet/nativeaot")]
    public static IRazorComponentsBuilder AddServerComponents(this IRazorComponentsBuilder builder, Action<CircuitOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        builder.Services.AddDataProtection();

        builder.Services.TryAddScoped<ProtectedLocalStorage>();
        builder.Services.TryAddScoped<ProtectedSessionStorage>();

        // This call INTENTIONALLY uses the AddHubOptions on the SignalR builder, because it will merge
        // the global HubOptions before running the configure callback. We want to ensure that happens
        // once. Our AddHubOptions method doesn't do this.
        //
        // We need to restrict the set of protocols used by default to our specialized one. Users have
        // the chance to modify options further via the builder.
        //
        // Other than the options, the things exposed by the SignalR builder aren't very meaningful in
        // the Server-Side Blazor context and thus aren't exposed.
        builder.Services.AddSignalR().AddHubOptions<ComponentHub>(options =>
        {
            options.SupportedProtocols.Clear();
            options.SupportedProtocols.Add(BlazorPackHubProtocol.ProtocolName);
        });

        // Register the Blazor specific hub protocol
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol, BlazorPackHubProtocol>());

        // Here we add a bunch of services that don't vary in any way based on the
        // user's configuration. So even if the user has multiple independent server-side
        // Components entrypoints, this lot is the same and repeated registrations are a no-op.
        builder.Services.TryAddSingleton<ICircuitFactory, CircuitFactory>();
        builder.Services.TryAddSingleton<IServerComponentDeserializer, ServerComponentDeserializer>();
        builder.Services.TryAddSingleton<ICircuitHandleRegistry, CircuitHandleRegistry>();
        builder.Services.TryAddSingleton<RootComponentTypeCache>();
        builder.Services.TryAddSingleton<ComponentParameterDeserializer>();
        builder.Services.TryAddSingleton<ComponentParametersTypeCache>();
        builder.Services.TryAddSingleton<CircuitIdFactory>();
        builder.Services.TryAddScoped<IErrorBoundaryLogger, RemoteErrorBoundaryLogger>();

        builder.Services.TryAddScoped(s => s.GetRequiredService<ICircuitAccessor>().Circuit);
        builder.Services.TryAddScoped<ICircuitAccessor, DefaultCircuitAccessor>();

        builder.Services.TryAddSingleton<CircuitRegistry>();

        // Standard blazor hosting services implementations
        //
        // These intentionally replace the non-interactive versions included in MVC.
        builder.Services.AddScoped<NavigationManager, RemoteNavigationManager>();
        builder.Services.AddScoped<IJSRuntime, RemoteJSRuntime>();
        builder.Services.AddScoped<INavigationInterception, RemoteNavigationInterception>();
        builder.Services.AddScoped<IScrollToLocationHash, RemoteScrollToLocationHash>();
        builder.Services.TryAddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CircuitOptions>, CircuitOptionsJSInteropDetailedErrorsConfiguration>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CircuitOptions>, CircuitOptionsJavaScriptInitializersConfiguration>());

        // Binding sources
        builder.Services.TryAddScoped<FormDataProvider, DefaultFormDataProvider>();

        if (configure != null)
        {
            builder.Services.Configure(configure);
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<RenderModeEndpointProvider, CircuitEndpointProvider>());

        return builder;
    }

    private class CircuitEndpointProvider : RenderModeEndpointProvider
    {
        public CircuitEndpointProvider(IServiceProvider services)
        {
            Services = services;
        }

        public IServiceProvider Services { get; }

        public override IEnumerable<RouteEndpointBuilder> GetEndpointBuilders(
            IComponentRenderMode renderMode,
            IApplicationBuilder applicationBuilder)
        {
            var endpointRouteBuilder = new EndpointRouteBuilder(Services, applicationBuilder);
            endpointRouteBuilder.MapBlazorHub();

            return endpointRouteBuilder.GetEndpoints();
        }

        public override bool Supports(IComponentRenderMode renderMode)
        {
            return renderMode switch
            {
                ServerRenderMode _ or AutoRenderMode _ => true,
                _ => false,
            };
        }

        private class EndpointRouteBuilder : IEndpointRouteBuilder
        {
            private readonly IApplicationBuilder _applicationBuilder;

            public EndpointRouteBuilder(IServiceProvider serviceProvider, IApplicationBuilder applicationBuilder)
            {
                ServiceProvider = serviceProvider;
                _applicationBuilder = applicationBuilder;
            }

            public IServiceProvider ServiceProvider { get; }

            public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>() { };

            public IApplicationBuilder CreateApplicationBuilder()
            {
                return _applicationBuilder.New();
            }

            internal IEnumerable<RouteEndpointBuilder> GetEndpoints()
            {
                foreach (var ds in DataSources)
                {
                    foreach (var endpoint in ds.Endpoints)
                    {
                        var routeEndpoint = (RouteEndpoint)endpoint;
                        var builder = new RouteEndpointBuilder(endpoint.RequestDelegate, routeEndpoint.RoutePattern, routeEndpoint.Order);
                        for (var i = 0; i < routeEndpoint.Metadata.Count; i++)
                        {
                            var metadata = routeEndpoint.Metadata[i];
                            builder.Metadata.Add(metadata);
                        }

                        yield return builder;
                    }
                }
            }
        }
    }
}
