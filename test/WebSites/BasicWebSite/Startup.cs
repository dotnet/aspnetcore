using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace BasicWebSite
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
            // Set up application services
            app.UseServices(services =>
            {
                // Add MVC services to the services container
                services.AddMvc();
            });

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute("ActionAsMethod", "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
