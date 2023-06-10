// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Binding;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.DependencyInjection;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides methods for registering services required for server-side rendering of Razor Components.
/// </summary>
public static class RazorComponentsServiceCollectionExtensions
{
    /// <summary>
    /// Registers services required for server-side rendering of Razor Components.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A builder for configuring the Razor Components endpoints.</returns>
    [RequiresUnreferencedCode("Razor Components does not currently support native AOT.", Url = "https://aka.ms/aspnet/nativeaot")]
    public static IRazorComponentsBuilder AddRazorComponents(this IServiceCollection services)
    {
        services.TryAddSingleton<RazorComponentsMarkerService>();

        // Results
        services.TryAddSingleton<RazorComponentResultExecutor>();

        // Endpoints
        services.TryAddSingleton<RazorComponentEndpointDataSourceFactory>();
        services.TryAddSingleton<RazorComponentEndpointFactory>();

        // Common services required for components server side rendering
        services.TryAddSingleton<ServerComponentSerializer>(services => new ServerComponentSerializer(services.GetRequiredService<IDataProtectionProvider>()));
        services.TryAddSingleton<WebAssemblyComponentSerializer>();
        services.TryAddScoped<EndpointHtmlRenderer>();
        services.TryAddScoped<IComponentPrerenderer>(services => services.GetRequiredService<EndpointHtmlRenderer>());
        services.TryAddScoped<NavigationManager, HttpNavigationManager>();
        services.TryAddScoped<IJSRuntime, UnsupportedJavaScriptRuntime>();
        services.TryAddScoped<INavigationInterception, UnsupportedNavigationInterception>();
        services.TryAddScoped<IScrollToLocationHash, UnsupportedScrollToLocationHash>();
        services.TryAddScoped<ComponentStatePersistenceManager>();
        services.TryAddScoped<PersistentComponentState>(sp => sp.GetRequiredService<ComponentStatePersistenceManager>().State);
        services.TryAddScoped<IErrorBoundaryLogger, PrerenderingErrorBoundaryLogger>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<RazorComponentsEndpointsOptions>, RazorComponentsEndpointsDetailedErrorsConfiguration>());
        services.TryAddScoped<EndpointRoutingStateProvider>();
        services.TryAddScoped<IRoutingStateProvider>(sp => sp.GetRequiredService<EndpointRoutingStateProvider>());

        // Form handling
        services.TryAddScoped<FormDataProvider, HttpContextFormDataProvider>();
        services.TryAddScoped<IFormValueSupplier, DefaultFormValuesSupplier>();

        return new DefaultRazorComponentsBuilder(services);
    }

    private sealed class DefaultRazorComponentsBuilder : IRazorComponentsBuilder
    {
        public DefaultRazorComponentsBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
