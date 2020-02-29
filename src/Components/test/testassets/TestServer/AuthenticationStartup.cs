using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
    public class AuthenticationStartupBase
    {
        private readonly Action<IEndpointRouteBuilder> _configureMode;

        public AuthenticationStartupBase(IConfiguration configuration, Action<IEndpointRouteBuilder> configureMode)
        {
            Configuration = configuration;
            _configureMode = configureMode;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

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

            app.UseAuthentication();

            // Mount the server-side Blazor app on /subdir
            app.Map("/subdir", app =>
            {
                app.UseStaticFiles();
                app.UseClientSideBlazorFiles<BasicTestApp.Program>();

                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapRazorPages();
                    endpoints.MapBlazorHub();
                    _configureMode(endpoints);
                });
            });
        }
    }

    public class AuthenticationStartup : AuthenticationStartupBase
    {
        public AuthenticationStartup(IConfiguration configuration)
            : base(configuration, (endpoints) => endpoints.MapFallbackToClientSideBlazor<BasicTestApp.Program>("index.html"))
        {
        }
    }

    public class ServerAuthenticationStartup : AuthenticationStartupBase
    {
        public ServerAuthenticationStartup(IConfiguration configuration)
            : base(configuration, (endpoints) => endpoints.MapFallbackToPage("/_ServerHost"))
        {
        }
    }

}
