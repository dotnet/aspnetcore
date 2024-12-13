// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Endpoints.DependencyInjection;
using Microsoft.AspNetCore.Components.Endpoints.Forms;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server;
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
    /// <param name="configure">An <see cref="Action{RazorComponentOptions}"/> to configure the provided <see cref="RazorComponentsServiceOptions"/>.</param>
    /// <returns>An <see cref="IRazorComponentsBuilder"/> that can be used to further configure the Razor component services.</returns>
    [RequiresUnreferencedCode("Razor Components does not currently support trimming or native AOT.", Url = "https://aka.ms/aspnet/nativeaot")]
    public static IRazorComponentsBuilder AddRazorComponents(this IServiceCollection services, Action<RazorComponentsServiceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Dependencies
        services.AddLogging();
        services.AddAntiforgery();

        services.TryAddSingleton<RazorComponentsMarkerService>();

        // Endpoints
        services.TryAddSingleton<RazorComponentEndpointDataSourceFactory>();
        services.TryAddSingleton<RazorComponentEndpointFactory>();
        if (MetadataUpdater.IsSupported)
        {
            services.TryAddSingleton<HotReloadService>();
        }
        services.TryAddScoped<IRazorComponentEndpointInvoker, RazorComponentEndpointInvoker>();

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
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPostConfigureOptions<RazorComponentsServiceOptions>, DefaultRazorComponentsServiceOptionsConfiguration>());
        services.TryAddScoped<EndpointRoutingStateProvider>();
        services.TryAddScoped<IRoutingStateProvider>(sp => sp.GetRequiredService<EndpointRoutingStateProvider>());
        services.TryAddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
        services.AddSupplyValueFromQueryProvider();
        services.TryAddCascadingValue(sp => sp.GetRequiredService<EndpointHtmlRenderer>().HttpContext);

        services.TryAddScoped<ResourceCollectionProvider>();

        // Form handling
        services.AddSupplyValueFromFormProvider();
        services.TryAddScoped<AntiforgeryStateProvider, EndpointAntiforgeryStateProvider>();
        services.TryAddScoped<HttpContextFormDataProvider>();
        services.TryAddScoped<IFormValueMapper, HttpContextFormValueMapper>();

        if (configure != null)
        {
            services.Configure(configure);
        }

        return new DefaultRazorComponentsBuilder(services);
    }

    private sealed class DefaultRazorComponentsBuilder(IServiceCollection services) : IRazorComponentsBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}
