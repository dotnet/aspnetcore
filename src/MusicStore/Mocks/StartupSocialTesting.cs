using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;
using Microsoft.AspNet.Security.Facebook;
using Microsoft.AspNet.Security.Google;
using Microsoft.AspNet.Security.Twitter;
using Microsoft.AspNet.Security.MicrosoftAccount;
using Microsoft.AspNet.Security;
using Microsoft.Framework.Cache.Memory;
using MusicStore.Mocks.Common;
using MusicStore.Mocks.Facebook;
using MusicStore.Mocks.Twitter;
using MusicStore.Mocks.Google;
using Microsoft.Framework.Runtime;
using System.Threading.Tasks;
using MusicStore.Mocks.MicrosoftAccount;

namespace MusicStore
{
    public class StartupSocialTesting
    {
        public void Configure(IApplicationBuilder app)
        {
            Console.WriteLine("Social Testing mode...");
            //Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
            //then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            var configuration = new Configuration()
                        .AddJsonFile("config.json")
                        .AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.

            //Error page middleware displays a nice formatted HTML page for any unhandled exceptions in the request pipeline.
            //Note: ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production.
            app.UseErrorPage(ErrorPageOptions.ShowAll);

            app.UsePerRequestServices(services =>
            {
                //If this type is present - we're on mono
                var runningOnMono = Type.GetType("Mono.Runtime") != null;

                // Add EF services to the services container
                if (runningOnMono)
                {
                    services.AddEntityFramework()
                            .AddInMemoryStore();
                }
                else
                {
                    services.AddEntityFramework()
                            .AddSqlServer();
                }

                services.AddScoped<MusicStoreContext>();

                // Configure DbContext           
                services.ConfigureOptions<MusicStoreDbContextOptions>(options =>
                {
                    options.DefaultAdminUserName = configuration.Get("DefaultAdminUsername");
                    options.DefaultAdminPassword = configuration.Get("DefaultAdminPassword");
                    if (runningOnMono)
                    {
                        options.UseInMemoryStore();
                    }
                    else
                    {
                        options.UseSqlServer(configuration.Get("Data:DefaultConnection:ConnectionString"));
                    }
                });

                // Add Identity services to the services container
                services.AddDefaultIdentity<MusicStoreContext, ApplicationUser, IdentityRole>(configuration);

                services.ConfigureFacebookAuthentication(options =>
                {
                    options.AppId = "[AppId]";
                    options.AppSecret = "[AppSecret]";
                    options.Notifications = new FacebookAuthenticationNotifications()
                    {
                        OnAuthenticated = FacebookNotifications.OnAuthenticated,
                        OnReturnEndpoint = FacebookNotifications.OnReturnEndpoint,
                        OnApplyRedirect = FacebookNotifications.OnApplyRedirect
                    };
                    options.BackchannelHttpHandler = new FacebookMockBackChannelHttpHandler();
                    options.StateDataFormat = new CustomStateDataFormat();
                    options.Scope.Add("email");
                    options.Scope.Add("read_friendlists");
                    options.Scope.Add("user_checkins");
                });

                // Add MVC services to the services container
                services.AddMvc();

                //Add all SignalR related services to IoC.
                services.AddSignalR();

                //Add InMemoryCache
                //Currently not able to AddSingleTon
                services.AddInstance<IMemoryCache>(new MemoryCache());
            });

            //To gracefully shutdown the server - Not for production scenarios
            app.Map("/shutdown", shutdown =>
            {
                shutdown.Run(async context =>
                {
                    var appShutdown = context.ApplicationServices.GetService<IApplicationShutdown>();
                    appShutdown.RequestShutdown();

                    await Task.Delay(10 * 1000, appShutdown.ShutdownRequested);
                    if (appShutdown.ShutdownRequested.IsCancellationRequested)
                    {
                        await context.Response.WriteAsync("Shutting down gracefully");
                    }
                    else
                    {
                        await context.Response.WriteAsync("Shutting down token not fired");
                    }
                });
            });

            //Configure SignalR
            app.UseSignalR();

            // Add static files to the request pipeline
            app.UseStaticFiles();

            // Add cookie-based authentication to the request pipeline
            app.UseIdentity();

            app.UseFacebookAuthentication();

            app.UseGoogleAuthentication(options => 
            {
                options.ClientId = "[ClientId]";
                options.ClientSecret = "[ClientSecret]";
                options.AccessType = "offline";
                options.Notifications = new GoogleAuthenticationNotifications()
                {
                    OnAuthenticated = GoogleNotifications.OnAuthenticated,
                    OnReturnEndpoint = GoogleNotifications.OnReturnEndpoint,
                    OnApplyRedirect = GoogleNotifications.OnApplyRedirect
                };
                options.StateDataFormat = new CustomStateDataFormat();
                options.BackchannelHttpHandler = new GoogleMockBackChannelHttpHandler();
            });

            app.UseTwitterAuthentication(options =>
            {
                options.ConsumerKey = "[ConsumerKey]";
                options.ConsumerSecret = "[ConsumerSecret]";
                options.Notifications = new TwitterAuthenticationNotifications()
                {
                    OnAuthenticated = TwitterNotifications.OnAuthenticated,
                    OnReturnEndpoint = TwitterNotifications.OnReturnEndpoint,
                    OnApplyRedirect = TwitterNotifications.OnApplyRedirect
                };
                options.StateDataFormat = new CustomTwitterStateDataFormat();
                options.BackchannelHttpHandler = new TwitterMockBackChannelHttpHandler();
#if ASPNET50
                options.BackchannelCertificateValidator = null;
#endif
            });

            app.UseMicrosoftAccountAuthentication(options =>
            {
                options.Caption = "MicrosoftAccount - Requires project changes";
                options.ClientId = "[ClientId]";
                options.ClientSecret = "[ClientSecret]";
                options.Notifications = new MicrosoftAccountAuthenticationNotifications()
                {
                    OnAuthenticated = MicrosoftAccountNotifications.OnAuthenticated,
                    OnReturnEndpoint = MicrosoftAccountNotifications.OnReturnEndpoint,
                    OnApplyRedirect = MicrosoftAccountNotifications.OnApplyRedirect
                };
                options.BackchannelHttpHandler = new MicrosoftAccountMockBackChannelHandler();
                options.StateDataFormat = new CustomStateDataFormat();
                options.Scope.Add("wl.basic");
                options.Scope.Add("wl.signin");
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