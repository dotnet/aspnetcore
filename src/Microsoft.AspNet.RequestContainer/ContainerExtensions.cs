// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.RequestContainer;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    public static class ContainerExtensions
    {
        public static IApplicationBuilder UseRequestServices(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ContainerMiddleware>();
        }

        public static IApplicationBuilder UseRequestServices(this IApplicationBuilder builder, IServiceProvider applicationServices)
        {
            builder.ApplicationServices = applicationServices;

            return builder.UseMiddleware<ContainerMiddleware>();
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

            return builder.UseMiddleware<ContainerMiddleware>();
        }
    }
}