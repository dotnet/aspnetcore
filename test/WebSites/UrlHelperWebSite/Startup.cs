using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

namespace UrlHelperWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();
            configuration.AddJsonFile("config.json");

            // Set up application services
            app.UsePerRequestServices(services =>
            {
                services.ConfigureOptions<AppOptions>(optionsSetup =>
                {
                    optionsSetup.ServeCDNContent = Convert.ToBoolean(configuration.Get("ServeCDNContent"));
                    optionsSetup.CDNServerBaseUrl = configuration.Get("CDNServerBaseUrl");
                    optionsSetup.GenerateLowercaseUrls = Convert.ToBoolean(configuration.Get("GenerateLowercaseUrls"));
                });

                // Add MVC services to the services container
                services.AddMvc(configuration);

                services.AddScoped<IUrlHelper, CustomUrlHelper>();
            });

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute("Default", "{controller=Home}/{action=Index}");
            });
        }
    }
}
