// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Routing.Template;

namespace Microsoft.AspNet.Routing
{
    public static class RouteCollectionExtensions
    {
        public static IRouteCollection MapRoute(this IRouteCollection routes, string template)
        {
            MapRoute(routes, template, null);
            return routes;
        }

        public static IRouteCollection MapRoute(this IRouteCollection routes, string template, object defaults)
        {
            MapRoute(routes, template, new RouteValueDictionary(defaults));
            return routes;
        }

        public static IRouteCollection MapRoute(this IRouteCollection routes, string template, IDictionary<string, object> defaults)
        {
            if (routes.DefaultHandler == null)
            {
                throw new ArgumentException("DefaultHandler must be set.");
            }

            routes.Add(new TemplateRoute(routes.DefaultHandler, template, defaults));
            return routes;
        }
    }
}
