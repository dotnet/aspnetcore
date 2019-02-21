// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Components.Server.DependencyInjection;
using Microsoft.AspNetCore.Components.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for components.
    /// </summary>
    public static class RazorComponentsServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Razor Component app services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddRazorComponents(this IServiceCollection services)
        {
            services.AddSignalR().AddMessagePackProtocol();

            // Here we add a bunch of services that don't vary in any way based on the
            // user's configuration. So even if the user has multiple independent server-side
            // Components entrypoints, this lot is the same and repeated registrations are a no-op.
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<StaticFileOptions>, ConfigureStaticFilesOptions>());
            services.TryAddSingleton<CircuitFactory, DefaultCircuitFactory>();
            services.TryAddScoped(s => s.GetRequiredService<ICircuitAccessor>().Circuit);
#pragma warning disable CS0618 // Type or member is obsolete
            services.TryAddScoped<IJSRuntimeAccessor, DefaultJSRuntimeAccessor>();
#pragma warning restore CS0618 // Type or member is obsolete
            services.TryAddScoped<ICircuitAccessor, DefaultCircuitAccessor>();

            // We explicitly take over the prerrendering and components services here.
            // We can't have two separate component implementations coexisting at the
            // same time, so when you register components (Circuits) it takes over
            // all the abstractions.
            services.AddScoped<IComponentPrerrenderer, CircuitPrerrenderer>();

            // Standard razor component services implementations
            services.AddScoped<IUriHelper, RemoteUriHelper>();
            services.AddScoped<IJSRuntime, RemoteJSRuntime>();

            return services;
        }
    }
}
