// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Routing
{
    public static class RouteBuilderExtensions
    {
        public static IRouteBuilder MapRoute(this IRouteBuilder routeCollectionBuilder,
                                             string name,
                                             string template)
        {
            MapRoute(routeCollectionBuilder, name, template, defaults: null);
            return routeCollectionBuilder;
        }

        public static IRouteBuilder MapRoute(this IRouteBuilder routeCollectionBuilder,
                                             string name,
                                             string template,
                                             object defaults)
        {
            MapRoute(routeCollectionBuilder, name, template, new RouteValueDictionary(defaults));
            return routeCollectionBuilder;
        }

        public static IRouteBuilder MapRoute(this IRouteBuilder routeCollectionBuilder,
                                             string name,
                                             string template,
                                             IDictionary<string, object> defaults)
        {
            if (routeCollectionBuilder.DefaultHandler == null)
            {
                throw new InvalidOperationException(Resources.DefaultHandler_MustBeSet);
            }

            var inlineConstraintResolver = routeCollectionBuilder
                                                    .ServiceProvider
                                                    .GetService<IInlineConstraintResolver>();

            routeCollectionBuilder.Routes.Add(new TemplateRoute(routeCollectionBuilder.DefaultHandler,
                                              name,
                                              template,
                                              defaults,
                                              constraints: null,
                                              inlineConstraintResolver: inlineConstraintResolver));
            return routeCollectionBuilder;
        }

        public static IRouteBuilder MapRoute(this IRouteBuilder routeCollectionBuilder,
                                             string name,
                                             string template,
                                             object defaults,
                                             object constraints)
        {
            MapRoute(routeCollectionBuilder,
                     name,
                     template,
                     new RouteValueDictionary(defaults),
                     new RouteValueDictionary(constraints));
            return routeCollectionBuilder;
        }

        public static IRouteBuilder MapRoute(this IRouteBuilder routeCollectionBuilder,
                                             string name,
                                             string template,
                                             object defaults,
                                             IDictionary<string, object> constraints)
        {
            MapRoute(routeCollectionBuilder, name, template, new RouteValueDictionary(defaults), constraints);
            return routeCollectionBuilder;
        }

        public static IRouteBuilder MapRoute(this IRouteBuilder routeCollectionBuilder,
                                             string name,
                                             string template,
                                             IDictionary<string, object> defaults, object constraints)
        {
            MapRoute(routeCollectionBuilder, name, template, defaults, new RouteValueDictionary(constraints));
            return routeCollectionBuilder;
        }

        public static IRouteBuilder MapRoute(this IRouteBuilder routeCollectionBuilder,
                                             string name,
                                             string template,
                                             IDictionary<string, object> defaults,
                                             IDictionary<string, object> constraints)
        {
            if (routeCollectionBuilder.DefaultHandler == null)
            {
                throw new InvalidOperationException(Resources.DefaultHandler_MustBeSet);
            }

            var inlineConstraintResolver = routeCollectionBuilder
                                                        .ServiceProvider
                                                        .GetService<IInlineConstraintResolver>();
            routeCollectionBuilder.Routes.Add(new TemplateRoute(routeCollectionBuilder.DefaultHandler,
                                                                name,
                                                                template,
                                                                defaults,
                                                                constraints,
                                                                inlineConstraintResolver));
            return routeCollectionBuilder;
        }
    }
}
