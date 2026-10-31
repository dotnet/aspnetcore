// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace AuthSamples.PathSchemeSelection;

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
        services.AddMvc();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, ApiAuthHandler>("Api", o => { })
            .AddCookie(options =>
            {
                // Foward any requests that start with /api to that scheme
                options.ForwardDefaultSelector = ctx =>
                {
                    return ctx.Request.Path.StartsWithSegments("/api") ? "Api" : null;
                };
                options.AccessDeniedPath = "/account/denied";
                options.LoginPath = "/account/login";
            });
    }

    public class ApiAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ClaimsPrincipal _id;

        public ApiAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
            var id = new ClaimsIdentity("Api");
            id.AddClaim(new Claim(ClaimTypes.Name, "Hao", ClaimValueTypes.String, "Api"));
            _id = new ClaimsPrincipal(id);
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(_id, "Api")));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            endpoints.MapControllerRoute(
                name: "api",
                pattern: "api/{controller=Home}/{action=Index}/{id?}");
        });
    }
}
