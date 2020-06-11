using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace CustomAuthorizationFailureResponse.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Decorate<TServiceType, TServiceImplementation>(this IServiceCollection services)
        {
            var descriptors = services.Where(descriptor => descriptor.ServiceType == typeof(TServiceType)).ToList();
            foreach(var descriptor in descriptors)
            {
                var index = services.IndexOf(descriptor);
                services[index] = ServiceDescriptor.Describe(typeof(TServiceType), provider => ActivatorUtilities.CreateInstance(provider, typeof(TServiceImplementation), ActivatorUtilities.GetServiceOrCreateInstance(provider, descriptor.ImplementationType)), descriptor.Lifetime);
            }

            return services;
        }
    }
}
