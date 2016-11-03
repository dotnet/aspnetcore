using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static ISignalRBuilder AddSignalR(this IServiceCollection services)
        {
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultHubLifetimeManager<>));
            services.AddSingleton(typeof(IHubContext<>), typeof(HubContext<>));
            services.AddSingleton(typeof(HubEndPoint<>), typeof(HubEndPoint<>));
            services.AddSingleton(typeof(RpcEndpoint<>), typeof(RpcEndpoint<>));
            services.AddSingleton<IConfigureOptions<SignalROptions>, SignalROptionsSetup>();
            services.AddSingleton<JsonNetInvocationAdapter>();
            services.AddSingleton<InvocationAdapterRegistry>();

            return new SignalRBuilder(services);
        }

        public static ISignalRBuilder AddSignalROptions(this ISignalRBuilder builder, Action<SignalROptions> configure)
        {
            builder.Services.Configure(configure);
            return builder;
        }
    }
}
