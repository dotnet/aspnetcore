// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.DependencyInjection;

namespace RoutingSample.Web
{
    public static class RouteBuilderExtensions
    {
        public static IRouteBuilder AddPrefixRoute(this IRouteBuilder routeBuilder,
                                                   string prefix)
        {
            if (routeBuilder.DefaultHandler == null)
            {
                throw new InvalidOperationException("DefaultHandler must be set.");
            }

            if (routeBuilder.ServiceProvider == null)
            {
                throw new InvalidOperationException("ServiceProvider must be set.");
            }

            return AddPrefixRoute(routeBuilder, prefix, routeBuilder.DefaultHandler);
        }

        public static IRouteBuilder AddPrefixRoute(this IRouteBuilder routeBuilder,
                                                   string prefix,
                                                   IRouter handler)
        {
            routeBuilder.Routes.Add(new PrefixRoute(handler, prefix));
            return routeBuilder;
        }

        public static IRouteBuilder MapLocaleRoute(
            this IRouteBuilder routeBuilder,
            string locale,
            string routeTemplate,
            object defaults)
        {
            var defaultsDictionary = new RouteValueDictionary(defaults);
            defaultsDictionary.Add("locale", locale);

            var constraintResolver = routeBuilder.ServiceProvider.GetService<IInlineConstraintResolver>();

            var route = new TemplateRoute(
                target: routeBuilder.DefaultHandler,
                routeTemplate: routeTemplate,
                defaults: defaultsDictionary,
                constraints: null,
                dataTokens: null,
                inlineConstraintResolver: constraintResolver);
            routeBuilder.Routes.Add(route);

            return routeBuilder;
        }
    }
}