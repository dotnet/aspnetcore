// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace RoutingSample.Web
{
    public static class RouteBuilderExtensions
    {
        public static IRouteBuilder AddPrefixRoute(
            this IRouteBuilder routeBuilder,
            string prefix,
            IRouteHandler handler)
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

            var route = new Route(
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