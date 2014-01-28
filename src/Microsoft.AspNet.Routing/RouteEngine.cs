
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.AspNet.Routing
{
    public class RouteEngine : IRouteEngine
    {
        public RouteEngine(RouteTable routes)
        {
            this.Routes = routes;
        }

        public RouteTable Routes
        {
            get;
            private set;
        }

        public RouteMatch GetMatch(IDictionary<string, object> context)
        {
            var routingContext = new RoutingContext(context);

            for (int i = 0; i < this.Routes.Routes.Count; i++)
            {
                var route = this.Routes.Routes[i];

                var match = route.GetMatch(routingContext);
                if (match == null)
                {
                    continue;
                }

                context.SetRouteMatchValues(match.Values);
                context.SetRouteEngine(this);

                return match;
            }

            return null;
        }

        public BoundRoute GetUrl(string name, IDictionary<string, object> context, IDictionary<string, object> values)
        {
            var bindingContext = new RouteBindingContext(context, values);

            IRoute route;

            if (!String.IsNullOrWhiteSpace(name))
            {
                if (Routes.NamedRoutes.TryGetValue(name, out route))
                {
                    return route.Bind(bindingContext);
                }
            }

            for (int j = 0; j < this.Routes.Routes.Count; j++)
            {
                route = this.Routes.Routes[j];

                var result = route.Bind(bindingContext);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
