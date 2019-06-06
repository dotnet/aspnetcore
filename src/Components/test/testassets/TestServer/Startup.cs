using BasicTestApp;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddNewtonsoftJson();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", _ => { /* Controlled below */ });
            });
            services.AddServerSideBlazor();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("NameMustStartWithB", policy =>
                    policy.RequireAssertion(ctx => ctx.User.Identity.Name?.StartsWith("B") ?? false));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // It's not enough just to return "Access-Control-Allow-Origin: *", because
            // browsers don't allow wildcards in conjunction with credentials. So we must
            // specify explicitly which origin we want to allow.
            app.UseCors(policy =>
            {
                policy.SetIsOriginAllowed(host => host.StartsWith("http://localhost:") || host.StartsWith("http://127.0.0.1:"))
                    .AllowAnyHeader()
                    .WithExposedHeaders("MyCustomHeader")
                    .AllowAnyMethod()
                    .AllowCredentials();
            });

            app.UseAuthentication();

            // Mount the server-side Blazor app on /subdir
            app.Map("/subdir", subdirApp =>
            {
                subdirApp.UseClientSideBlazorFiles<BasicTestApp.Startup>();

                subdirApp.UseRouting();

                subdirApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapBlazorHub(typeof(Index), selector: "root");
                    endpoints.MapFallbackToClientSideBlazor<BasicTestApp.Startup>("index.html");
                });
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });

            // Separately, mount a prerendered server-side Blazor app on /prerendered
            app.Map("/prerendered", subdirApp =>
            {
                subdirApp.UsePathBase("/prerendered");
                subdirApp.UseStaticFiles();
                subdirApp.UseRouting();
                subdirApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToPage("/PrerenderedHost");
                    endpoints.MapBlazorHub();
                });
            });
        }
    }
}
