
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Routing.Owin
{
    public class RouterMiddleware
    {
        public RouterMiddleware(Func<IDictionary<string, object>, Task> next, IRouteEngine engine, RouteTable routes)
        {
            this.Next = next;
            this.Engine = engine;
            this.Routes = routes;
        }

        private IRouteEngine Engine
        {
            get;
            set;
        }

        private Func<IDictionary<string, object>, Task> Next
        {
            get;
            set;
        }

        private RouteTable Routes
        {
            get;
            set;
        }

        public Task Invoke(IDictionary<string, object> context)
        {
            var match = this.Engine.GetMatch(context);
            if (match == null)
            {
                return Next.Invoke(context);
            }
            else
            {
                return match.Destination.Invoke(context);
            }
        }
    }
}