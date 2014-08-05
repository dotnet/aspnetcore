using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;

namespace RazorWebSite
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            // Set up application services
            app.UseServices(services =>
            {
                // Add MVC services to the services container
                services.AddMvc(configuration);
                services.AddTransient<InjectedHelper>();
            });

            // Add MVC to the request pipeline
            app.UseMvc();
        }
    }
}
