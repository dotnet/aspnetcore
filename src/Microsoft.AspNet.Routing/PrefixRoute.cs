using System;

namespace Microsoft.AspNet.Routing
{
    public class PrefixRoute : IRoute
    {
        public PrefixRoute(IRouteEndpoint endpoint, string prefix)
        {
            this.Endpoint = endpoint;

            if (prefix != null)
            {
                if (prefix.Length == 0 || prefix[0] != '/')
                {
                    prefix = '/' + prefix;
                }

                if (prefix.Length > 1 && prefix[prefix.Length - 1] == '/')
                {
                    prefix = prefix.Substring(0, prefix.Length - 1);
                }

                this.Prefix = prefix;
            }
        }

        private IRouteEndpoint Endpoint
        {
            get;
            set;
        }

        private string Prefix
        {
            get;
            set;
        }

        public virtual BoundRoute Bind(RouteBindingContext context)
        {
            return null;
        }

        public virtual RouteMatch GetMatch(RoutingContext context)
        {
            if (this.Prefix == null)
            {
                return new RouteMatch(this.Endpoint.AppFunc);
            }
            else if (context.RequestPath.StartsWith(this.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                if (context.RequestPath.Length > this.Prefix.Length)
                {
                    char next = context.RequestPath[this.Prefix.Length];
                    if (next != '/' && next != '#' && next != '?')
                    {
                        return null;
                    }
                }

                return new RouteMatch(this.Endpoint.AppFunc);
            }

            return null;
        }
    }
}
