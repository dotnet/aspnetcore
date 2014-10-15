using System;
using Autofac;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Autofac;

namespace AutofacWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services => {
                services.AddMvc(configuration);
                services.AddTransient<HelloWorldBuilder>();

                var builder = new ContainerBuilder();
                AutofacRegistration.Populate(builder, 
                                             services, 
                                             fallbackServiceProvider: app.ApplicationServices);

                var container = builder.Build();

                return container.Resolve<IServiceProvider>();
            });

            app.UseMvc(routes =>
            {
                // This default route is for running the project directly.
                routes.MapRoute("default", "{controller=DI}/{action=Index}");
            });
        }
    }
}
