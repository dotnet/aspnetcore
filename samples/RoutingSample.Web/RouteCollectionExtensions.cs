using System;
using Microsoft.AspNet.Routing;

namespace RoutingSample.Web
{
    public static class RouteCollectionExtensions
    {
        public static IRouteCollection AddPrefixRoute(this IRouteCollection routes, string prefix)
        {
            if (routes.DefaultHandler == null)
            {
                throw new ArgumentException("DefaultHandler must be set.");
            }

            return AddPrefixRoute(routes, prefix, routes.DefaultHandler);
        }

        public static IRouteCollection AddPrefixRoute(this IRouteCollection routes, string prefix, IRouter handler)
        {
            routes.Add(new PrefixRoute(handler, prefix));
            return routes;
        }
    }
}
