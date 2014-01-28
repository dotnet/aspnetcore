
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#if NET45
using Owin;
#endif

namespace Microsoft.AspNet.Routing.Owin
{
    internal class RouteBuilder : IRouteBuilder
    {
#if NET45
        public RouteBuilder(IAppBuilder builder, IRouteEngine engine, RouteTable routes)
        {
            this.AppBuilder = builder;
            this.Engine = engine;
            this.Routes = routes;
        }
#else
        public RouteBuilder(IRouteEngine engine, RouteTable routes)
        {
            this.Engine = engine;
            this.Routes = routes;
        }
#endif

#if NET45
        public IAppBuilder AppBuilder
        {
            get;
            private set;
        }
#endif

        public IRouteEngine Engine
        {
            get;
            private set;
        }

        private RouteTable Routes
        {
            get;
            set;
        }

        public IRouteEndpoint ForApp(Func<Func<IDictionary<string, object>, Task>> handlerFactory)
        {
            return new RouteEndpoint(handlerFactory(), this.Routes);
        }
    }
}