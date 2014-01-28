using System;

namespace Microsoft.AspNet.Routing.HttpMethod
{
    public class HttpMethodRoute : PrefixRoute
    {
        public HttpMethodRoute(IRouteEndpoint endpoint, string prefix, string method)
            : base(endpoint, prefix)
        {
            this.Method = method;
        }

        private string Method
        {
            get;
            set;
        }

        public override RouteMatch GetMatch(RoutingContext context)
        {
            if (String.Equals(context.RequestMethod, this.Method, StringComparison.OrdinalIgnoreCase))
            {
                return base.GetMatch(context);
            }

            return null;
        }
    }
}
