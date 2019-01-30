// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    public static class MvcEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapApplication(
            this IEndpointRouteBuilder routeBuilder)
        {
            return MapActionDescriptors(routeBuilder, null);
        }

        public static IEndpointConventionBuilder MapAssembly<TContainingType>(
            this IEndpointRouteBuilder routeBuilder)
        {
            return MapActionDescriptors(routeBuilder, typeof(TContainingType));
        }

        private static IEndpointConventionBuilder MapActionDescriptors(
            this IEndpointRouteBuilder routeBuilder,
            Type containingType)
        {
            var mvcEndpointDataSource = routeBuilder.DataSources.OfType<MvcEndpointDataSource>().FirstOrDefault();

            if (mvcEndpointDataSource == null)
            {
                mvcEndpointDataSource = routeBuilder.ServiceProvider.GetRequiredService<MvcEndpointDataSource>();
                routeBuilder.DataSources.Add(mvcEndpointDataSource);
            }

            var conventionBuilder = new DefaultEndpointConventionBuilder();

            var assemblyFilter = containingType?.Assembly;

            mvcEndpointDataSource.AttributeRoutingConventionResolvers.Add(actionDescriptor =>
            {
                // Filter a descriptor by the assembly
                // Note that this will only filter actions on controllers
                // Does not support filtering Razor pages embedded in assemblies
                if (assemblyFilter != null)
                {
                    if (actionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
                    {
                        if (controllerActionDescriptor.ControllerTypeInfo.Assembly != assemblyFilter)
                        {
                            return null;
                        }
                    }
                }

                return conventionBuilder;
            });

            return conventionBuilder;
        }

        public static IEndpointConventionBuilder MapControllerRoute(
            this IEndpointRouteBuilder routeBuilder,
            string name,
            string template)
        {
            return MapControllerRoute(routeBuilder, name, template, defaults: null);
        }

        public static IEndpointConventionBuilder MapControllerRoute(
            this IEndpointRouteBuilder routeBuilder,
            string name,
            string template,
            object defaults)
        {
            return MapControllerRoute(routeBuilder, name, template, defaults, constraints: null);
        }

        public static IEndpointConventionBuilder MapControllerRoute(
            this IEndpointRouteBuilder routeBuilder,
            string name,
            string template,
            object defaults,
            object constraints)
        {
            return MapControllerRoute(routeBuilder, name, template, defaults, constraints, dataTokens: null);
        }

        public static IEndpointConventionBuilder MapControllerRoute(
            this IEndpointRouteBuilder routeBuilder,
            string name,
            string template,
            object defaults,
            object constraints,
            object dataTokens)
        {
            var mvcEndpointDataSource = routeBuilder.DataSources.OfType<MvcEndpointDataSource>().FirstOrDefault();

            if (mvcEndpointDataSource == null)
            {
                mvcEndpointDataSource = routeBuilder.ServiceProvider.GetRequiredService<MvcEndpointDataSource>();
                routeBuilder.DataSources.Add(mvcEndpointDataSource);
            }

            var endpointInfo = new MvcEndpointInfo(
                name,
                template,
                new RouteValueDictionary(defaults),
                new RouteValueDictionary(constraints),
                new RouteValueDictionary(dataTokens),
                routeBuilder.ServiceProvider.GetRequiredService<ParameterPolicyFactory>());

            mvcEndpointDataSource.ConventionalEndpointInfos.Add(endpointInfo);

            return endpointInfo;
        }
    }
}
