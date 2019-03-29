using BasicTestApp;
using BasicTestApp.RouterTest;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Hosting;
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
            services.AddRazorComponents();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            AllowCorsForAnyLocalhostPort(app);

            app.UseRouting();

            // Mount the server-side Blazor app on /subdir
            app.Map("/subdir", subdirApp =>
            {
                // The following two lines are equivalent to:
                //     endpoints.MapComponentsHub<Index>();
                //
                // However it's expressed using routing as a way of checking that
                // we're not relying on any extra magic inside MapComponentsHub, since it's
                // important that people can set up these bits of middleware manually (e.g., to
                // swap in UseAzureSignalR instead of UseSignalR).

                subdirApp.UseRouting();

                subdirApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<ComponentHub>(ComponentHub.DefaultPath).AddComponent<Index>(selector: "root");
                });

                subdirApp.MapWhen(
                    ctx => ctx.Features.Get<IEndpointFeature>()?.Endpoint == null,
                    blazorBuilder => blazorBuilder.UseBlazor<BasicTestApp.Startup>());
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
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
                    endpoints.MapComponentHub<TestRouter>("app");
                });
            });
        }

        private static void AllowCorsForAnyLocalhostPort(IApplicationBuilder app)
        {
            // It's not enough just to return "Access-Control-Allow-Origin: *", because
            // browsers don't allow wildcards in conjunction with credentials. So we must
            // specify explicitly which origin we want to allow.
            app.Use((context, next) =>
            {
                if (context.Request.Headers.TryGetValue("origin", out var incomingOriginValue))
                {
                    var origin = incomingOriginValue.ToArray()[0];
                    if (origin.StartsWith("http://localhost:") || origin.StartsWith("http://127.0.0.1:"))
                    {
                        context.Response.Headers.Add("Access-Control-Allow-Origin", origin);
                        context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                        context.Response.Headers.Add("Access-Control-Allow-Methods", "HEAD,GET,PUT,POST,DELETE,OPTIONS");
                        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type,TestHeader,another-header");
                        context.Response.Headers.Add("Access-Control-Expose-Headers", "MyCustomHeader,TestHeader,another-header");
                    }
                }

                return next();
            });
        }
    }
}
