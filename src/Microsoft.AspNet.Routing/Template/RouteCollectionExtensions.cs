// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Routing.Template
{
    public static class RouteCollectionExtensions
    {
        public static IRouteCollection AddTemplateRoute(this IRouteCollection routes, string template)
        {
            AddTemplateRoute(routes, template, null);
            return routes;
        }

        public static IRouteCollection AddTemplateRoute(this IRouteCollection routes, string template, object defaults)
        {
            AddTemplateRoute(routes, template, new RouteValueDictionary(defaults));
            return routes;
        }

        public static IRouteCollection AddTemplateRoute(this IRouteCollection routes, string template, IDictionary<string, object> defaults)
        {
            routes.Add(new TemplateRoute(routes.DefaultHandler, template, defaults));
            return routes;
        }
    }
}
