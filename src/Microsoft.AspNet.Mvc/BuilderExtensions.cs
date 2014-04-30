using System;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;

namespace Microsoft.AspNet
{
    public static class BuilderExtensions
    {
        public static IBuilder UseMvc([NotNull] this IBuilder app)
        {
            return app.UseMvc(routes =>
            {
                routes.MapRoute(null, "{area}/{controller}/{action}", new { controller = "Home", action = "Index" });
                routes.MapRoute(null, "{area}/{controller}", new { controller = "Home" });
                routes.MapRoute(null, "{controller}/{action}", new { controller = "Home", action = "Index" });
                routes.MapRoute(null, "{controller}", new { controller = "Home" });
            });
        }

        public static IBuilder UseMvc([NotNull] this IBuilder app, [NotNull] Action<IRouteCollection> configureRoutes)
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