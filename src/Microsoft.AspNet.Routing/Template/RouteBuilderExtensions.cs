// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Routing.Template
{
    public static class RouteBuilderExtensions
    {
        public static void AddTemplateRoute(this IRouteBuilder builder, string template)
        {
            AddTemplateRoute(builder, template, null);
        }

        public static void AddTemplateRoute(this IRouteBuilder builder, string template, IDictionary<string, object> defaults)
        {
            builder.Routes.Add(new TemplateRoute(builder.Endpoint, template, defaults));
        }

        public static void AddTemplateRoute(this IRouteBuilder builder, string template, object defaults)
        {
            builder.Routes.Add(new TemplateRoute(builder.Endpoint, template, new RouteValueDictionary(defaults)));
        }
    }
}
