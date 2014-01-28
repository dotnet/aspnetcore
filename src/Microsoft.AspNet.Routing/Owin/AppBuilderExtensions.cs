
#if NET45

using Owin;

namespace Microsoft.AspNet.Routing.Owin
{
    public static class AppBuilderExtensions
    {
        public static IRouteBuilder UseRouter(this IAppBuilder builder)
        {
            var routes = new RouteTable();
            var engine = new RouteEngine(routes);

            var next = builder.Use(typeof(RouterMiddleware), engine, routes);

            return new RouteBuilder(next, engine, routes); 
        }
    }
}

#endif
