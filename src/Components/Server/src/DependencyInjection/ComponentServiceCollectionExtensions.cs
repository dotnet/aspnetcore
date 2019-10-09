// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.BlazorPack;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection
{
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
        public static IServerSideBlazorBuilder AddServerSideBlazor(this IServiceCollection services, Action<CircuitOptions> configure = null)
        {
            var builder = new DefaultServerSideBlazorBuilder(services);

            services.AddDataProtection();

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
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<StaticFileOptions>, ConfigureStaticFilesOptions>());
            services.TryAddSingleton<CircuitFactory>();
            services.TryAddSingleton<ServerComponentDeserializer>();
            services.TryAddSingleton<ServerComponentTypeCache>();
            services.TryAddSingleton<ComponentParameterDeserializer>();
            services.TryAddSingleton<ComponentParametersTypeCache>();
            services.TryAddSingleton<CircuitIdFactory>();

            services.TryAddScoped(s => s.GetRequiredService<ICircuitAccessor>().Circuit);
            services.TryAddScoped<ICircuitAccessor, DefaultCircuitAccessor>();

            services.TryAddSingleton<CircuitRegistry>();

            // Standard blazor hosting services implementations
            //
            // These intentionally replace the non-interactive versions included in MVC.
            services.AddScoped<NavigationManager, RemoteNavigationManager>();
            services.AddScoped<IJSRuntime, RemoteJSRuntime>();
            services.AddScoped<INavigationInterception, RemoteNavigationInterception>();
            services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<CircuitOptions>, CircuitOptionsJSInteropDetailedErrorsConfiguration>());

            if (configure != null)
            {
                services.Configure(configure);
            }

            return builder;
        }

        private class DefaultServerSideBlazorBuilder : IServerSideBlazorBuilder
        {
            public DefaultServerSideBlazorBuilder(IServiceCollection services)
            {
                Services = services;
            }

            public IServiceCollection Services { get; }
        }
    }
}
