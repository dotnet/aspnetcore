// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Template;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Builder
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
            return MapRoute(routeCollectionBuilder, name, template, defaults, constraints: null);
        }

        public static IRouteBuilder MapRoute(this IRouteBuilder routeCollectionBuilder,
                                             string name,
                                             string template,
                                             object defaults,
                                             object constraints)
        {
            return MapRoute(routeCollectionBuilder, name, template, defaults, constraints, dataTokens: null);
        }

        public static IRouteBuilder MapRoute(this IRouteBuilder routeCollectionBuilder,
                                             string name,
                                             string template,
                                             object defaults,
                                             object constraints,
                                             object dataTokens)
        {
            if (routeCollectionBuilder.DefaultHandler == null)
            {
                throw new InvalidOperationException(Resources.DefaultHandler_MustBeSet);
            }

            var inlineConstraintResolver = routeCollectionBuilder
                                                        .ServiceProvider
                                                        .GetRequiredService<IInlineConstraintResolver>();
            routeCollectionBuilder.Routes.Add(new TemplateRoute(routeCollectionBuilder.DefaultHandler,
                                                                name,
                                                                template,
                                                                ObjectToDictionary(defaults),
                                                                ObjectToDictionary(constraints),
                                                                ObjectToDictionary(dataTokens),
                                                                inlineConstraintResolver));

            return routeCollectionBuilder;
        }

        private static IDictionary<string, object> ObjectToDictionary(object value)
        {
            var dictionary = value as IDictionary<string, object>;
            if (dictionary != null)
            {
                return dictionary;
            }

            return new RouteValueDictionary(value);
        }
    }
}
