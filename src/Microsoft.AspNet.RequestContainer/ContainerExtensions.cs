// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
        public static IBuilder UseMiddleware<T>(this IBuilder builder, params object[] args)
        {
            return builder.UseMiddleware(typeof(T), args);
        }

        public static IBuilder UseMiddleware(this IBuilder builder, Type middleware, params object[] args)
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
            return builder.UseServices(serviceCollection =>
            {
                configureServices(serviceCollection);
                return serviceCollection.BuildServiceProvider(builder.ApplicationServices);
            });
        }

        public static IBuilder UseServices(this IBuilder builder, Func<ServiceCollection, IServiceProvider> configureServices)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.Add(OptionsServices.GetDefaultServices());
            builder.ApplicationServices = configureServices(serviceCollection);

            return builder.UseMiddleware(typeof(ContainerMiddleware));
        }
    }
}