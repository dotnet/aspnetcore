using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
    // WARNING: DO NOT MODIFY THIS STARTUP CLASS FOR TEST PURPOSES
    // Most of the client-side tests are executed using the developer host directly, so changing values here won't
    // affect any client-side test.
    public class ClientStartup
    {
        public ClientStartup(IConfiguration configuration)
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
                // Add it before to ensure it takes priority over files in wwwroot
                app.UseClientSideBlazorFiles<BasicTestApp.Program>();
                app.UseStaticFiles();

                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                    endpoints.MapControllers();
                    endpoints.MapFallbackToClientSideBlazor<BasicTestApp.Program>("index.html");
                });
            });
        }
    }
}
