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
            // We've discussed with the SignalR team and believe it's OK to have repeated
            // calls to AddSignalR (making the nonfirst ones no-ops). If we want to change
            // this in the future, we could change AddComponents to be an extension
            // method on ISignalRServerBuilder so the developer always has to chain it onto
            // their own AddSignalR call. For now we're keeping it like this because it's
            // simpler for developers in common cases.
            services.AddSignalR().AddMessagePackProtocol();

            AddStandardRazorComponentsServices(services);

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
            services.AddScoped<IComponentPrerrenderer, CircuitPrerrenderer>();
            return services;
        }

        private static void AddStandardRazorComponentsServices(IServiceCollection services)
        {
            services.AddScoped<IUriHelper, RemoteUriHelper>();
            services.AddScoped<IJSRuntime, RemoteJSRuntime>();
        }
    }
}
