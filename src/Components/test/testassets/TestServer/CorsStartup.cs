using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
    public class CorsStartup
    {
        public CorsStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddCors(options =>
            {
                // It's not enough just to return "Access-Control-Allow-Origin: *", because
                // browsers don't allow wildcards in conjunction with credentials. So we must
                // specify explicitly which origin we want to allow.

                options.AddPolicy("AllowAll", policy => policy
                    .SetIsOriginAllowed(host => host.StartsWith("http://localhost:") || host.StartsWith("http://127.0.0.1:"))
                    .AllowAnyHeader()
                    .WithExposedHeaders("MyCustomHeader")
                    .AllowAnyMethod()
                    .AllowCredentials());
            });
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

                app.UseRouting();

                app.UseCors();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapFallbackToClientSideBlazor<BasicTestApp.Startup>("index.html");
                });
            });
        }
    }
}
