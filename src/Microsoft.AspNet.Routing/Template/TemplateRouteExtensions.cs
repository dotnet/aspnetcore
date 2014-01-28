
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing.Template
{
    public static class HttpMethodRouteExtensions
    {
        public static IRouteEndpoint AddTemplateRoute(this IRouteEndpoint endpoint, string template)
        {
            return endpoint.AddTemplateRoute(null, template, null, null, null);
        }

        public static IRouteEndpoint AddTemplateRoute(this IRouteEndpoint endpoint, string template, IDictionary<string, object> defaults)
        {
            return endpoint.AddTemplateRoute(null, template, defaults, null, null);
        }

        public static IRouteEndpoint AddTemplateRoute(this IRouteEndpoint endpoint, string template, IDictionary<string, object> defaults, IDictionary<string, object> constraints)
        {
            return endpoint.AddTemplateRoute(null, template, defaults, constraints, null);
        }

        public static IRouteEndpoint AddTemplateRoute(this IRouteEndpoint endpoint, string template, IDictionary<string, object> defaults, IDictionary<string, object> constraints, IDictionary<string, object> data)
        {
            return endpoint.AddTemplateRoute(null, template, defaults, constraints, data);
        }

        public static IRouteEndpoint AddTemplateRoute(this IRouteEndpoint endpoint, string name, string template)
        {
            return endpoint.AddTemplateRoute(name, template, null, null, null);
        }

        public static IRouteEndpoint AddTemplateRoute(this IRouteEndpoint endpoint, string name, string template, IDictionary<string, object> defaults)
        {
            return endpoint.AddTemplateRoute(name, template, defaults, null, null);
        }

        public static IRouteEndpoint AddTemplateRoute(this IRouteEndpoint endpoint, string name, string template, IDictionary<string, object> defaults, IDictionary<string, object> constraints)
        {
            return endpoint.AddTemplateRoute(name, template, defaults, constraints, null);
        }

        public static IRouteEndpoint AddTemplateRoute(this IRouteEndpoint endpoint, string name, string template, IDictionary<string, object> defaults, IDictionary<string, object> constraints, IDictionary<string, object> data)
        {
            return endpoint.AddRoute(name, new TemplateRoute(endpoint, template, defaults, constraints, data));
        }
    }
}
