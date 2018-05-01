// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>.
    /// </summary>
    public static class SignalRDependencyInjectionExtensions
    {
        /// <summary>
        /// Adds the minimum essential SignalR services to the specified <see cref="IServiceCollection" />. Additional services
        /// must be added separately using the <see cref="ISignalRServerBuilder"/> returned from this method.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        /// <returns>An <see cref="ISignalRServerBuilder"/> that can be used to further configure the SignalR services.</returns>
        public static ISignalRServerBuilder AddSignalRCore(this IServiceCollection services)
        {
            services.AddSingleton<SignalRMarkerService>();
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultHubLifetimeManager<>));
            services.AddSingleton(typeof(IHubProtocolResolver), typeof(DefaultHubProtocolResolver));
            services.AddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.AddSingleton(typeof(IHubContext<,>), typeof(HubContext<,>));
            services.AddSingleton(typeof(HubConnectionHandler<>), typeof(HubConnectionHandler<>));
            services.AddSingleton(typeof(IUserIdProvider), typeof(DefaultUserIdProvider));
            services.AddSingleton(typeof(HubDispatcher<>), typeof(DefaultHubDispatcher<>));
            services.AddScoped(typeof(IHubActivator<>), typeof(DefaultHubActivator<>));
            services.AddAuthorization();

            var builder = new SignalRServerBuilder(services);
            builder.AddJsonProtocol();
            return builder;
        }
    }
}
