using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
    public class ServerStartup
    {
        public ServerStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddServerSideBlazor();
            services.AddSingleton<ResourceRequestLog>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResourceRequestLog resourceRequestLog)
        {
            var enUs = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = enUs;
            CultureInfo.DefaultThreadCurrentUICulture = enUs;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Mount the server-side Blazor app on /subdir
            app.Map("/subdir", app =>
            {
                app.Use((context, next) =>
                {
                    if (context.Request.Path.Value.EndsWith("/images/blazor_logo_1000x.png", StringComparison.Ordinal))
                    {
                        resourceRequestLog.AddRequest(context.Request);
                    }

                    return next(context);
                });

                app.UseStaticFiles();

                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapBlazorHub();
                    endpoints.MapControllerRoute("mvc", "{controller}/{action}");
                    endpoints.MapFallbackToPage("/_ServerHost");
                });
            });
        }
    }
}
