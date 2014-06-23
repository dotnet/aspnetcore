using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace RoutingWebSite
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);

                services.AddScoped<TestResponseGenerator>();
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute("areaRoute",
                                "{area:exists}/{controller}/{action}",
                                new { controller = "Home", action = "Index" });

                routes.MapRoute("ActionAsMethod", "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
