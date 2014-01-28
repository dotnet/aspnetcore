
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing.Owin
{
    internal class RouteEndpoint : IRouteEndpoint
    {
        public RouteEndpoint(Func<IDictionary<string, object>, Task> appFunc, RouteTable routes)
        {
            this.AppFunc = appFunc;
            this.Routes = routes;
        }

        public Func<IDictionary<string, object>, Task> AppFunc
        {
            get;
            private set;
        }

        private RouteTable Routes
        {
            get;
            set;
        }

        public IRouteEndpoint AddRoute(string name, IRoute route)
        {
            this.Routes.Add(name, route);
            return this;
        }
    }
}