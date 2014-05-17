// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Routing
{
    public static class RouteCollectionExtensions
    {
        public static IRouteCollection MapRoute(this IRouteCollection routes, string name, string template)
        {
            MapRoute(routes, name, template, defaults: null);
            return routes;
        }

        public static IRouteCollection MapRoute(this IRouteCollection routes, string name, string template,
                                                object defaults)
        {
            MapRoute(routes, name, template, new RouteValueDictionary(defaults));
            return routes;
        }

        public static IRouteCollection MapRoute(this IRouteCollection routes, string name, string template,
                                                IDictionary<string, object> defaults)
        {
            if (routes.DefaultHandler == null)
            {
                throw new InvalidOperationException(Resources.DefaultHandler_MustBeSet);
            }

            routes.Add(new TemplateRoute(routes.DefaultHandler,
                                         name,
                                         template,
                                         defaults,
                                         constraints: null,
                                         inlineConstraintResolver: routes.InlineConstraintResolver));
            return routes;
        }

        public static IRouteCollection MapRoute(this IRouteCollection routes, string name, string template,
                                            object defaults, object constraints)
        {
            MapRoute(routes,
                     name,
                     template,
                     new RouteValueDictionary(defaults),
                     new RouteValueDictionary(constraints));
            return routes;
        }

        public static IRouteCollection MapRoute(this IRouteCollection routes, string name, string template,
                                                object defaults, IDictionary<string, object> constraints)
        {
            MapRoute(routes, name, template, new RouteValueDictionary(defaults), constraints);
            return routes;
        }

        public static IRouteCollection MapRoute(this IRouteCollection routes, string name, string template,
                                                IDictionary<string, object> defaults, object constraints)
        {
            MapRoute(routes, name, template, defaults, new RouteValueDictionary(constraints));
            return routes;
        }

        public static IRouteCollection MapRoute(this IRouteCollection routes,
                                                string name,
                                                string template,
                                                IDictionary<string, object> defaults,
                                                IDictionary<string, object> constraints)
        {
            if (routes.DefaultHandler == null)
            {
                throw new InvalidOperationException(Resources.DefaultHandler_MustBeSet);
            }
            
            routes.Add(new TemplateRoute(routes.DefaultHandler,
                                         name,
                                         template,
                                         defaults,
                                         constraints,
                                         routes.InlineConstraintResolver));
            return routes;
        }
    }
}
