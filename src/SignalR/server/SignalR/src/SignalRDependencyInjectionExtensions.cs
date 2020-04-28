// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up SignalR services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class SignalRDependencyInjectionExtensions
    {
        /// <summary>
        /// Adds hub specific options to an <see cref="ISignalRServerBuilder"/>.
        /// </summary>
        /// <typeparam name="THub">The hub type to configure.</typeparam>
        /// <param name="signalrBuilder">The <see cref="ISignalRServerBuilder"/>.</param>
        /// <param name="configure">A callback to configure the hub options.</param>
        /// <returns>The same instance of the <see cref="ISignalRServerBuilder"/> for chaining.</returns>
        public static ISignalRServerBuilder AddHubOptions<THub>(this ISignalRServerBuilder signalrBuilder, Action<HubOptions<THub>> configure) where THub : Hub
        {
            if (signalrBuilder == null)
            {
                throw new ArgumentNullException(nameof(signalrBuilder));
            }

            signalrBuilder.Services.AddSingleton<IConfigureOptions<HubOptions<THub>>, HubOptionsSetup<THub>>();
            signalrBuilder.Services.Configure(configure);
            return signalrBuilder;
        }

        /// <summary>
        /// Adds SignalR services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>An <see cref="ISignalRServerBuilder"/> that can be used to further configure the SignalR services.</returns>
        public static ISignalRServerBuilder AddSignalR(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddConnections();
            // Disable the WebSocket keep alive since SignalR has it's own
            services.Configure<WebSocketOptions>(o => o.KeepAliveInterval = TimeSpan.Zero);
            services.TryAddSingleton<SignalRMarkerService>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<HubOptions>, HubOptionsSetup>());
            return services.AddSignalRCore();
        }

        /// <summary>
        /// Adds SignalR services to the specified <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <param name="configure">An <see cref="Action{HubOptions}"/> to configure the provided <see cref="HubOptions"/>.</param>
        /// <returns>An <see cref="ISignalRServerBuilder"/> that can be used to further configure the SignalR services.</returns>
        public static ISignalRServerBuilder AddSignalR(this IServiceCollection services, Action<HubOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var signalrBuilder = services.AddSignalR();
            // Setup users settings after we've setup ours
            services.Configure(configure);
            return signalrBuilder;
        }
    }
}
