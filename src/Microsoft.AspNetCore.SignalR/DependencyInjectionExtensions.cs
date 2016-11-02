using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static ISignalRBuilder AddSignalR(this IServiceCollection services)
        {
            services.AddSingleton(typeof(HubLifetimeManager<>), typeof(DefaultHubLifetimeManager<>));
            services.AddSingleton(typeof(HubEndPoint<>), typeof(HubEndPoint<>));
            services.AddSingleton(typeof(RpcEndpoint<>), typeof(RpcEndpoint<>));

            return new SignalRBuilder(services);
        }
    }

    public interface ISignalRBuilder
    {
        IServiceCollection Services { get; }
    }

    public class SignalRBuilder : ISignalRBuilder
    {
        public SignalRBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
