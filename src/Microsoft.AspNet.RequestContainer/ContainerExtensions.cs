using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;

namespace Microsoft.AspNet.RequestContainer
{
    public static class ContainerExtensions
    {
        public static IBuilder UseMiddleware(this IBuilder builder, Type middleware, params object[] args)
        {
            // TODO: move this ext method someplace nice
            return builder.Use(next =>
            {
                var typeActivator = builder.ApplicationServices.GetService<ITypeActivator>();
                var instance = typeActivator.CreateInstance(builder.ApplicationServices, middleware, new[] { next }.Concat(args).ToArray());
                var methodinfo = middleware.GetTypeInfo().GetDeclaredMethod("Invoke");
                return (RequestDelegate)methodinfo.CreateDelegate(typeof(RequestDelegate), instance);
            });
        }

        public static IBuilder UseServices(this IBuilder builder)
        {
            return builder.UseMiddleware(typeof(ContainerMiddleware));
        }

        public static IBuilder UseServices(this IBuilder builder, IServiceProvider applicationServices)
        {
            builder.ApplicationServices = applicationServices;

            return builder.UseMiddleware(typeof(ContainerMiddleware));
        }

        public static IBuilder UseServices(this IBuilder builder, IEnumerable<IServiceDescriptor> applicationServices)
        {
            return builder.UseServices(services => services.Add(applicationServices));
        }

        public static IBuilder UseServices(this IBuilder builder, Action<ServiceCollection> configureServices)
        {
            var serviceCollection = new ServiceCollection();
            configureServices(serviceCollection);
            builder.ApplicationServices = serviceCollection.BuildServiceProvider(builder.ApplicationServices);

            return builder.UseMiddleware(typeof(ContainerMiddleware));
        }
    }
}