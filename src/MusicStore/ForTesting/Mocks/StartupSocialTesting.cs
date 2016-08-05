using System;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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
                            options.UseInMemoryDatabase());
            }
            else
            {
                services.AddDbContext<MusicStoreContext>(options =>
                            options.UseSqlServer(Configuration["Data:DefaultConnection:ConnectionString"]));
            }

            // Add Identity services to the services container
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                    {
                        options.Cookies.ApplicationCookie.AccessDeniedPath = new PathString("/Home/AccessDenied");
                    })
                    .AddEntityFrameworkStores<MusicStoreContext>()
                    .AddDefaultTokenProviders();

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
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(minLevel: LogLevel.Warning);

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
            app.UseIdentity();

            app.UseFacebookAuthentication(new FacebookOptions
            {
                AppId = "[AppId]",
                AppSecret = "[AppSecret]",
                Events = new OAuthEvents()
                {
                    OnCreatingTicket = TestFacebookEvents.OnCreatingTicket,
                    OnTicketReceived = TestFacebookEvents.OnTicketReceived,
                    OnRedirectToAuthorizationEndpoint = TestFacebookEvents.RedirectToAuthorizationEndpoint
                },
                BackchannelHttpHandler = new FacebookMockBackChannelHttpHandler(),
                StateDataFormat = new CustomStateDataFormat(),
                Scope = { "email", "read_friendlists", "user_checkins" }
            });

            app.UseGoogleAuthentication(new GoogleOptions
            {
                ClientId = "[ClientId]",
                ClientSecret = "[ClientSecret]",
                AccessType = "offline",
                Events = new OAuthEvents()
                {
                    OnCreatingTicket = TestGoogleEvents.OnCreatingTicket,
                    OnTicketReceived = TestGoogleEvents.OnTicketReceived,
                    OnRedirectToAuthorizationEndpoint = TestGoogleEvents.RedirectToAuthorizationEndpoint
                },
                StateDataFormat = new CustomStateDataFormat(),
                BackchannelHttpHandler = new GoogleMockBackChannelHttpHandler()
            });

            app.UseTwitterAuthentication(new TwitterOptions
            {
                ConsumerKey = "[ConsumerKey]",
                ConsumerSecret = "[ConsumerSecret]",
                Events = new TwitterEvents()
                {
                    OnCreatingTicket = TestTwitterEvents.OnCreatingTicket,
                    OnTicketReceived = TestTwitterEvents.OnTicketReceived,
                    OnRedirectToAuthorizationEndpoint = TestTwitterEvents.RedirectToAuthorizationEndpoint
                },
                StateDataFormat = new CustomTwitterStateDataFormat(),
                BackchannelHttpHandler = new TwitterMockBackChannelHttpHandler()
            });

            app.UseMicrosoftAccountAuthentication(new MicrosoftAccountOptions
            {
                DisplayName = "MicrosoftAccount - Requires project changes",
                ClientId = "[ClientId]",
                ClientSecret = "[ClientSecret]",
                Events = new OAuthEvents()
                {
                    OnCreatingTicket = TestMicrosoftAccountEvents.OnCreatingTicket,
                    OnTicketReceived = TestMicrosoftAccountEvents.OnTicketReceived,
                    OnRedirectToAuthorizationEndpoint = TestMicrosoftAccountEvents.RedirectToAuthorizationEndpoint
                },
                BackchannelHttpHandler = new MicrosoftAccountMockBackChannelHandler(),
                StateDataFormat = new CustomStateDataFormat(),
                Scope = { "wl.basic", "wl.signin" }
            });

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
