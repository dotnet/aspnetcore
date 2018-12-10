// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class ApplicationEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapApplication(this IEndpointRouteBuilder routeBuilder)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }

            var builders = new List<IEndpointConventionBuilder>();
            var factories = routeBuilder.ServiceProvider.GetRequiredService<IEnumerable<ApplicationDataSourceFactory>>();
            foreach (var factory in factories)
            {
                var builder = factory.GetOrCreateApplication(routeBuilder);
                if (builder != null)
                {
                    builders.Add(builder);
                }
            }

            return new CompositeEndpointConventionBuilder(builders);
        }
        
        public static IEndpointConventionBuilder MapAssembly<TContainingType>(this IEndpointRouteBuilder routeBuilder)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }

            return MapAssembly(routeBuilder, typeof(TContainingType).Assembly);
        }
        
        public static IEndpointConventionBuilder MapAssembly(this IEndpointRouteBuilder routeBuilder, Type containingType)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }

            if (containingType == null)
            {
                throw new ArgumentNullException(nameof(containingType));
            }

            return MapAssembly(routeBuilder, containingType.Assembly);
        }
        
        public static IEndpointConventionBuilder MapAssembly(this IEndpointRouteBuilder routeBuilder, Assembly assembly)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var builders = new List<IEndpointConventionBuilder>();
            var factories = routeBuilder.ServiceProvider.GetRequiredService<IEnumerable<ApplicationDataSourceFactory>>();
            foreach (var factory in factories)
            {
                var builder = factory.GetOrCreateAssembly(routeBuilder, assembly);
                if (builder != null)
                {
                    builders.Add(builder);
                }
            }

            return new CompositeEndpointConventionBuilder(builders);
        }
    }
}
