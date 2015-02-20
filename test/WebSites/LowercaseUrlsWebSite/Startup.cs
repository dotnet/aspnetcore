using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace LowercaseUrlsWebSite
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);

                services.Configure<RouteOptions>(routeOptions => routeOptions.LowercaseUrls = true);
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute("Default", "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
