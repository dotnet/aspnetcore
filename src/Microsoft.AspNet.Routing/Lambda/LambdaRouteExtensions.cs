using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace Microsoft.AspNet.Routing.Lambda
{
    public static class LambdaRouteExtensions
    {
        public static IRouteEndpoint On(this IRouteEndpoint endpoint, Func<IDictionary<string, object>, bool> condition)
        {
            endpoint.AddRoute(null, new LambdaRoute(endpoint, condition));
            return endpoint;
        }

        public static IRouteEndpoint On(this IRouteBuilder routeBuilder, Func<IDictionary<string, object>, bool> condition, AppFunc handler)
        {
            var endpoint = routeBuilder.ForApp(handler);
            return endpoint.On(condition);
        }
    }
}
