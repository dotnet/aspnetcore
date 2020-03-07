using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
    public class InternationalizationStartup
    {
        public InternationalizationStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddServerSideBlazor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Mount the server-side Blazor app on /subdir
            app.Map("/subdir", app =>
            {
                app.UseStaticFiles();
                app.UseClientSideBlazorFiles<BasicTestApp.Startup>();

                app.UseRequestLocalization(options =>
                {
                    options.AddSupportedCultures("en-US", "fr-FR");
                    options.AddSupportedUICultures("en-US", "fr-FR");

                    // Cookie culture provider is included by default, but we want it to be the only one.
                    options.RequestCultureProviders.Clear();
                    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());

                    // We want the default to be en-US so that the tests for bind can work consistently.
                    options.SetDefaultCulture("en-US");
                });

                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapBlazorHub();
                    endpoints.MapFallbackToPage("/_ServerHost");
                });
            });
        }
    }
}
