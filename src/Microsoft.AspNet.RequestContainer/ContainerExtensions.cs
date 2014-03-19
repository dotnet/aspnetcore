using System;
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
                //TODO: this should be MethodInfo.CreateDelegate for coreclr
                var typeActivator = builder.ServiceProvider.GetService<ITypeActivator>();
                var instance = typeActivator.CreateInstance(middleware, new[] { next }.Concat(args).ToArray());
                return (RequestDelegate)Delegate.CreateDelegate(typeof(RequestDelegate), instance, "Invoke");
            });
        }

        public static IBuilder UseContainer(this IBuilder app)
        {
            return app.UseMiddleware(typeof(ContainerMiddleware));
        }

        public static IBuilder UseContainer(this IBuilder app, IServiceProvider applicationServices)
        {
            app.ServiceProvider = applicationServices;

            return app.UseMiddleware(typeof(ContainerMiddleware));
        }

        public static IBuilder UseContainer(this IBuilder app, IEnumerable<IServiceDescriptor> applicationServices)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(applicationServices);
            app.ServiceProvider = serviceCollection.BuildServiceProvider(app.ServiceProvider);

            return app.UseMiddleware(typeof(ContainerMiddleware));
        }
    }
}