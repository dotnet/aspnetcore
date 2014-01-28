
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public static class RouteEngineExtensions
    {
        public static BoundRoute GetUrl(this IRouteEngine engine, IDictionary<string, object> context, IDictionary<string, object> values)
        {
            return engine.GetUrl(null, context, values);
        }

        public static BoundRoute GetUrl(this IRouteEngine engine, string name, IDictionary<string, object> context, IDictionary<string, object> values)
        {
            return engine.GetUrl(name, context, values);
        }
    }
}
