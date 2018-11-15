using System;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MusicStore.Components;
using MusicStore.Mocks.Common;
using MusicStore.Mocks.OpenIdConnect;
using MusicStore.Models;

namespace MusicStore
{
    public class StartupOpenIdConnectTesting
    {
        private readonly Platform _platform;

        public StartupOpenIdConnectTesting(IHostingEnvironment env)
        {
            //Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources,
            //then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("config.json")
                .AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.

            Configuration = builder.Build();
            _platform = new Platform();
        }

        public IConfiguration Configuration { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            // Add EF services to the services container
            if (_platform.UseInMemoryStore)
            {
                services.AddDbContext<MusicStoreContext>(options =>
                            options.UseInMemoryDatabase("Scratch"));
            }
            else
            {
                services.AddDbContext<MusicStoreContext>(options =>
                            options.UseSqlServer(Configuration["Data:DefaultConnection:ConnectionString"]));
            }

            // Add Identity services to the services container
            services.AddIdentity<ApplicationUser, IdentityRole>()
                    .AddEntityFrameworkStores<MusicStoreContext>()
                    .AddDefaultTokenProviders();

            // Create an Azure Active directory application and copy paste the following
            services.AddAuthentication().AddOpenIdConnect(options =>
            {
                options.Authority = "https://login.windows.net/[tenantName].onmicrosoft.com";
                options.ClientId = "c99497aa-3ee2-4707-b8a8-c33f51323fef";
                options.BackchannelHttpHandler = new OpenIdConnectBackChannelHttpHandler();
                options.StringDataFormat = new CustomStringDataFormat();
                options.StateDataFormat = new CustomStateDataFormat();
                options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                options.UseTokenLifetime = false;
                options.TokenValidationParameters.ValidateLifetime = false;
                options.ProtocolValidator.RequireNonce = true;
                options.ProtocolValidator.NonceLifetime = TimeSpan.FromDays(36500);

                options.Events = new OpenIdConnectEvents
                {
                    OnMessageReceived = TestOpenIdConnectEvents.MessageReceived,
                    OnAuthorizationCodeReceived = TestOpenIdConnectEvents.AuthorizationCodeReceived,
                    OnRedirectToIdentityProvider = TestOpenIdConnectEvents.RedirectToIdentityProvider,
                    OnTokenValidated = TestOpenIdConnectEvents.TokenValidated,
                };
            });

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder.WithOrigins("http://example.com");
                });
            });

            // Add MVC services to the services container
            services.AddMvc();

            //Add InMemoryCache
            services.AddSingleton<IMemoryCache, MemoryCache>();

            // Add session related services.
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();
            services.AddSession();

            // Add the system clock service
            services.AddSingleton<ISystemClock, SystemClock>();

            // Configure Auth
            services.Configure<AuthorizationOptions>(options =>
            {
                options.AddPolicy("ManageStore", new AuthorizationPolicyBuilder().RequireClaim("ManageStore", "Allowed").Build());
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            // force the en-US culture, so that the app behaves the same even on machines with different default culture
            var supportedCultures = new[] { new CultureInfo("en-US") };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            app.UseStatusCodePagesWithRedirects("~/Home/StatusCodePage");

            // Display custom error page in production when error occurs
            // During development use the ErrorPage middleware to display error information in the browser
            app.UseDeveloperExceptionPage();

            app.UseDatabaseErrorPage();

            // Configure Session.
            app.UseSession();

            // Add static files to the request pipeline
            app.UseStaticFiles();

            // Add authentication to the request pipeline
            app.UseAuthentication();

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "areaRoute",
                    template: "{area:exists}/{controller}/{action}",
                    defaults: new { action = "Index" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });

                routes.MapRoute(
                    name: "api",
                    template: "{controller}/{id?}");
            });

            //Populates the MusicStore sample data
            SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();
        }
    }
}
