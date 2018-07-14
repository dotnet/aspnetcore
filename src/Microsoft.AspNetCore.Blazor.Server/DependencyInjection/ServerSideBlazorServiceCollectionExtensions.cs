// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor;
using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.AspNetCore.Blazor.Server.Circuits;
using Microsoft.AspNetCore.Blazor.Services;
using Microsoft.JSInterop;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to configure an <see cref="IServiceCollection"/> for Server-Side Blazor.
    /// </summary>
    public static class ServerSideBlazorServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Server-Side Blazor services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddServerSideBlazor(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return AddServerSideBlazor(services, null, null);
        }

        /// <summary>
        /// Adds Server-Side Blazor services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">A delegate to configure the <see cref="ServerSideBlazorOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddServerSideBlazor(
            this IServiceCollection services,
            Action<ServerSideBlazorOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return AddServerSideBlazorCore(services, null, configure);
        }

        /// <summary>
        /// Adds Server-Side Blazor services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="startupType">A Blazor startup type.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddServerSideBlazor(this IServiceCollection services, Type startupType)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (startupType == null)
            {
                throw new ArgumentNullException(nameof(startupType));
            }

            return AddServerSideBlazorCore(services, startupType, null);
        }

        /// <summary>
        /// Adds Server-Side Blazor services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <typeparam name="TStartup">A Blazor startup type.</typeparam>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddServerSideBlazor<TStartup>(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return AddServerSideBlazorCore(services, typeof(TStartup), null);
        }

        /// <summary>
        /// Adds Server-Side Blazor services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="startupType">A Blazor startup type.</param>
        /// <param name="configure">A delegate to configure the <see cref="ServerSideBlazorOptions"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddServerSideBlazor(
            this IServiceCollection services,
            Type startupType,
            Action<ServerSideBlazorOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (startupType == null)
            {
                throw new ArgumentNullException(nameof(startupType));
            }

            return AddServerSideBlazorCore(services, startupType, configure);
        }

        /// <summary>
        /// Adds Server-Side Blazor services to the service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">A delegate to configure the <see cref="ServerSideBlazorOptions"/>.</param>
        /// <typeparam name="TStartup">A Blazor startup type.</typeparam>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddServerSideBlazor<TStartup>(
            this IServiceCollection services,
            Action<ServerSideBlazorOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return AddServerSideBlazorCore(services, typeof(TStartup), configure);
        }

        private static IServiceCollection AddServerSideBlazorCore(
            IServiceCollection services,
            Type startupType,
            Action<ServerSideBlazorOptions> configure)
        {
            services.AddSingleton<CircuitFactory, DefaultCircuitFactory>();
            services.AddScoped<ICircuitAccessor, DefaultCircuitAccessor>();
            services.AddScoped<Circuit>(s => s.GetRequiredService<ICircuitAccessor>().Circuit);

            services.AddScoped<IJSRuntimeAccessor, DefaultJSRuntimeAccessor>();
            services.AddScoped<IJSRuntime>(s => s.GetRequiredService<IJSRuntimeAccessor>().JSRuntime);

            services.AddScoped<IUriHelper, RemoteUriHelper>();

            services.AddSignalR().AddMessagePackProtocol(options =>
            {
                // TODO: Enable compression, either by having SignalR use
                // LZ4MessagePackSerializer instead of MessagePackSerializer,
                // or perhaps by compressing the RenderBatch bytes ourselves
                // and then using https://github.com/nodeca/pako in JS to decompress.
                options.FormatterResolvers.Insert(0, new RenderBatchFormatterResolver());
            });

            if (startupType != null)
            {
                // Make sure we only create a single instance of the startup type. We can register
                // it in the services so we can retrieve it later when creating the middlware.
                var startup = Activator.CreateInstance(startupType);
                services.AddSingleton(startupType, startup);

                // We don't need to reuse the wrapper, it's not stateful.
                var wrapper = new ConventionBasedStartup(startup);
                wrapper.ConfigureServices(services);
            }

            if (configure != null)
            {
                services.Configure(configure);
            }

            return services;
        }
    }
}
