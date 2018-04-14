// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class SignalRDependencyInjectionExtensions
    {
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
