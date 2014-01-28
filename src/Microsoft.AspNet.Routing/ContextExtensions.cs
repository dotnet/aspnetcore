
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public static class OwinExtensions
    {
        public static string EngineKey = "routing.Engine";
        public static string MatchValuesKey = "routing.Values";

        public static IRouteEngine GetRouteEngine(this IDictionary<string, object> context)
        {
            object obj;
            if (context.TryGetValue(EngineKey, out obj))
            {
                return obj as IRouteEngine;
            }

            return null;
        }

        public static void SetRouteEngine(this IDictionary<string, object> context, IRouteEngine value)
        {
            context[EngineKey] = value;
        }

        public static IDictionary<string, object> GetRouteMatchValues(this IDictionary<string, object> context)
        {
            object obj;
            if (context.TryGetValue(MatchValuesKey, out obj))
            {
                return obj as IDictionary<string, object>;
            }

            return null;
        }

        public static void SetRouteMatchValues(this IDictionary<string, object> context, IDictionary<string, object> values)
        {
            context[MatchValuesKey] = values;
        }
    }
}