using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Routing;


namespace Microsoft.AspNet.Mvc
{
    public static class BuilderExtensions
    {
        public static IBuilder UseMvc(this IBuilder app, Action<IRouteCollection> configureRoutes)
        {
            var routes = new RouteCollection
            {
                DefaultHandler = new MvcRouteHandler()
            };

            // REVIEW: Consider adding UseMvc() that automagically adds the default MVC route
            configureRoutes(routes);

            return app.UseRouter(routes);
        }
    }
}