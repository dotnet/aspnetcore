using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Routing.Template;

using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace Microsoft.AspNet.Routing.HttpMethod
{
    public static class HttpMethodRouteExtensions
    {
        public static IRouteEndpoint Get(this IRouteEndpoint endpoint)
        {
            endpoint.AddRoute(null, new HttpMethodRoute(endpoint, null, "GET"));
            return endpoint;
        }

        public static IRouteEndpoint Get(this IRouteBuilder routeBuilder, AppFunc handler)
        {
            var endpoint = routeBuilder.ForApp((next) => handler);
            return endpoint.Get();
        }

        public static IRouteEndpoint Get(this IRouteEndpoint endpoint, string prefix)
        {
            endpoint.AddRoute(null, new HttpMethodRoute(endpoint, prefix, "GET"));
            return endpoint;
        }

        public static IRouteEndpoint Get(this IRouteBuilder routeBuilder, string prefix, AppFunc handler)
        {
            var endpoint = routeBuilder.ForApp((next) => handler);
            return endpoint.Get(prefix);
        }

        public static IRouteEndpoint Post(this IRouteEndpoint endpoint)
        {
            endpoint.AddRoute(null, new HttpMethodRoute(endpoint, null, "POST"));
            return endpoint;
        }

        public static IRouteEndpoint Post(this IRouteBuilder routeBuilder, AppFunc handler)
        {
            var endpoint = routeBuilder.ForApp((next) => handler);
            return endpoint.Post();
        }

        public static IRouteEndpoint Post(this IRouteEndpoint endpoint, string prefix)
        {
            endpoint.AddRoute(null, new HttpMethodRoute(endpoint, prefix, "POST"));
            return endpoint;
        }

        public static IRouteEndpoint Post(this IRouteBuilder routeBuilder, string prefix, AppFunc handler)
        {
            var endpoint = routeBuilder.ForApp((next) => handler);
            return endpoint.Post(prefix);
        }
    }
}
