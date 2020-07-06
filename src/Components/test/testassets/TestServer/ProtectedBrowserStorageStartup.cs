using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
    public class ProtectedBrowserStorageStartup
    {
        public ProtectedBrowserStorageStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddServerSideBlazor();
            services.AddProtectedBrowserStorage();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Map("/subdir", app =>
            {
                app.UseBlazorFrameworkFiles();
                app.UseStaticFiles();
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
