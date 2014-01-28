using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing.Lambda
{
    public class LambdaRoute : IRoute
    {
        public LambdaRoute(IRouteEndpoint endpoint, Func<IDictionary<string, object>, bool> condition)
        {
            this.Endpoint = endpoint;
            this.Condition = condition;
        }

        private Func<IDictionary<string, object>, bool> Condition
        {
            get;
            set;
        }

        private IRouteEndpoint Endpoint
        {
            get;
            set;
        }

        public BoundRoute Bind(RouteBindingContext context)
        {
            return null;
        }

        public RouteMatch GetMatch(RoutingContext context)
        {
            if (Condition(context.Context))
            {
                return new RouteMatch(this.Endpoint.AppFunc);
            }

            return null;
        }
    }
}
