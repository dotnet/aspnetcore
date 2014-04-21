using Microsoft.AspNet;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Diagnostics;

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
                routes.MapRoute("{controller}/{action}", new { controller = "Home", action = "Index" });
            });

            app.UseWelcomePage();
        }       
    }
}