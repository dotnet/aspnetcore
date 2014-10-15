using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Framework.DependencyInjection;

namespace RazorWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            // Set up application services
            app.UseServices(services =>
            {
                // Add MVC services to the services container
                services.AddMvc(configuration);
                services.AddTransient<InjectedHelper>();
                services.Configure<RazorViewEngineOptions>(options =>
                {
                    var expander = new LanguageViewLocationExpander(
                            context => context.HttpContext.Request.Query["language-expander-value"]);
                    options.ViewLocationExpanders.Add(expander);
                });
            });

            // Add MVC to the request pipeline
            app.UseMvc();
        }
    }
}
