using System;
using System.Globalization;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicStore.Components;
using MusicStore.Mocks.Common;
using MusicStore.Mocks.Facebook;
using MusicStore.Mocks.Google;
using MusicStore.Mocks.MicrosoftAccount;
using MusicStore.Mocks.Twitter;
using MusicStore.Models;

namespace MusicStore
{
    public class StartupSocialTesting
    {
        private readonly Platform _platform;

        public StartupSocialTesting(IHostingEnvironment hostingEnvironment)
        {
            //Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources,
            //then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("config.json")
                .AddEnvironmentVariables() //All environment variables in the process's context flow in as configuration values.
                .AddJsonFile("configoverride.json", optional: true); // Used to override some configuration parameters that cannot be overridden by environment.

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

            services.ConfigureApplicationCookie(options => options.AccessDeniedPath = "/Home/AccessDenied");

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
            services.AddAuthorization(options =>
            {
                options.AddPolicy("ManageStore", new AuthorizationPolicyBuilder().RequireClaim("ManageStore", "Allowed").Build());
            });

            services.AddAuthentication()
                .AddFacebook(options =>
            {
                options.AppId = "[AppId]";
                options.AppSecret = "[AppSecret]";
                options.Events = new OAuthEvents()
                {
                    OnCreatingTicket = TestFacebookEvents.OnCreatingTicket,
                    OnTicketReceived = TestFacebookEvents.OnTicketReceived,
                    OnRedirectToAuthorizationEndpoint = TestFacebookEvents.RedirectToAuthorizationEndpoint
                };
                options.BackchannelHttpHandler = new FacebookMockBackChannelHttpHandler();
                options.StateDataFormat = new CustomStateDataFormat();
                options.Scope.Add("email");
                options.Scope.Add("read_friendlists");
                options.Scope.Add("user_checkins");
            }).AddGoogle(options =>
            {
                options.ClientId = "[ClientId]";
                options.ClientSecret = "[ClientSecret]";
                options.AccessType = "offline";
                options.Events = new OAuthEvents()
                {
                    OnCreatingTicket = TestGoogleEvents.OnCreatingTicket,
                    OnTicketReceived = TestGoogleEvents.OnTicketReceived,
                    OnRedirectToAuthorizationEndpoint = TestGoogleEvents.RedirectToAuthorizationEndpoint
                };
                options.StateDataFormat = new CustomStateDataFormat();
                options.BackchannelHttpHandler = new GoogleMockBackChannelHttpHandler();
            }).AddTwitter(options =>
            {
                options.ConsumerKey = "[ConsumerKey]";
                options.ConsumerSecret = "[ConsumerSecret]";
                options.Events = new TwitterEvents()
                {
                    OnCreatingTicket = TestTwitterEvents.OnCreatingTicket,
                    OnTicketReceived = TestTwitterEvents.OnTicketReceived,
                    OnRedirectToAuthorizationEndpoint = TestTwitterEvents.RedirectToAuthorizationEndpoint
                };
                options.StateDataFormat = new CustomTwitterStateDataFormat();
                options.BackchannelHttpHandler = new TwitterMockBackChannelHttpHandler();
            }).AddMicrosoftAccount(options =>
            {
                options.ClientId = "[ClientId]";
                options.ClientSecret = "[ClientSecret]";
                options.Events = new OAuthEvents()
                {
                    OnCreatingTicket = TestMicrosoftAccountEvents.OnCreatingTicket,
                    OnTicketReceived = TestMicrosoftAccountEvents.OnTicketReceived,
                    OnRedirectToAuthorizationEndpoint = TestMicrosoftAccountEvents.RedirectToAuthorizationEndpoint
                };
                options.BackchannelHttpHandler = new MicrosoftAccountMockBackChannelHandler();
                options.StateDataFormat = new CustomStateDataFormat();
                options.Scope.Add("wl.basic");
                options.Scope.Add("wl.signin");
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

            // Error page middleware displays a nice formatted HTML page for any unhandled exceptions in the request pipeline.
            // Note: Not recommended for production.
            app.UseDeveloperExceptionPage();

            app.UseDatabaseErrorPage();

            // Configure Session.
            app.UseSession();

            // Add static files to the request pipeline
            app.UseStaticFiles();

            // Add cookie-based authentication to the request pipeline
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
