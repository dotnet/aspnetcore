// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class ControllerEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapControllerType<TController>(this IEndpointRouteBuilder routeBuilder)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }

            var dataSource = GetOrCreateDataSource(routeBuilder);
            return dataSource.AddType(typeof(TController));
        }

        public static IEndpointConventionBuilder MapControllerType(this IEndpointRouteBuilder routeBuilder, Type controllerType)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }

            if (controllerType == null)
            {
                throw new ArgumentNullException(nameof(controllerType));
            }

            var dataSource = GetOrCreateDataSource(routeBuilder);
            return dataSource.AddType(controllerType);
        }

        public static void MapControllerRoute(
            this IEndpointRouteBuilder routeBuilder,
            string name,
            string template,
            object defaults = null,
            object constraints = null,
            object dataTokens = null)
        {
            var dataSource = GetOrCreateDataSource(routeBuilder);
            dataSource.AddConventionalRoute(new ConventionalRouteEntry(
                name,
                template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens),
                dataSource.NextConventionalRouteOrder));
        }

        private static ControllerEndpointDataSource GetOrCreateDataSource(IEndpointRouteBuilder routeBuilder)
        {
            var factory = routeBuilder.ServiceProvider
                .GetRequiredService<IEnumerable<ApplicationDataSourceFactory>>()
                .OfType<ControllerApplicationDataSourceFactory>()
                .FirstOrDefault();
            if (factory == null)
            {
                throw new InvalidOperationException("This method cannot be used without calling 'AddMvc()' or 'AddMvcCore()' on the service collection.");
            }

            return factory.GetOrCreateDataSource(routeBuilder);
        }
    }
}
