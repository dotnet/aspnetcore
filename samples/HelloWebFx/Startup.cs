using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace KWebStartup
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            app.UseErrorPage();

            app.UseServices(services =>
            {
                services.AddMvc();
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(null, "{controller}/{action}", new { controller = "Home", action = "Index" });
            });

            app.UseWelcomePage();
        }       
    }
}