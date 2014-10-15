// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.RequestContainer;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    public static class ContainerExtensions
    {
        public static IApplicationBuilder UseMiddleware<T>(this IApplicationBuilder builder, params object[] args)
        {
            return builder.UseMiddleware(typeof(T), args);
        }

        public static IApplicationBuilder UseMiddleware(this IApplicationBuilder builder, Type middleware, params object[] args)
        {
            // TODO: move this ext method someplace nice
            return builder.Use(next =>
            {
                var typeActivator = builder.ApplicationServices.GetService<ITypeActivator>();
                var instance = typeActivator.CreateInstance(builder.ApplicationServices, middleware, new[] { next }.Concat(args).ToArray());
                var methodinfo = middleware.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
                return (RequestDelegate)methodinfo.CreateDelegate(typeof(RequestDelegate), instance);
            });
        }

        public static IApplicationBuilder UseRequestServices(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware(typeof(ContainerMiddleware));
        }

        public static IApplicationBuilder UseRequestServices(this IApplicationBuilder builder, IServiceProvider applicationServices)
        {
            builder.ApplicationServices = applicationServices;

            return builder.UseMiddleware(typeof(ContainerMiddleware));
        }

        public static IApplicationBuilder UseServices(this IApplicationBuilder builder, IEnumerable<IServiceDescriptor> applicationServices)
        {
            return builder.UseServices(services => services.Add(applicationServices));
        }

        public static IApplicationBuilder UseServices(this IApplicationBuilder builder, Action<ServiceCollection> configureServices)
        {
            return builder.UseServices(serviceCollection =>
            {
                configureServices(serviceCollection);
                return serviceCollection.BuildServiceProvider(builder.ApplicationServices);
            });
        }

        public static IApplicationBuilder UseServices(this IApplicationBuilder builder, Func<ServiceCollection, IServiceProvider> configureServices)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.Add(OptionsServices.GetDefaultServices());
            builder.ApplicationServices = configureServices(serviceCollection);

            return builder.UseMiddleware(typeof(ContainerMiddleware));
        }
    }
}