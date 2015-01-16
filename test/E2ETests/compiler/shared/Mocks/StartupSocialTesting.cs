using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Diagnostics.Entity;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security;
using Microsoft.AspNet.Security.Facebook;
using Microsoft.AspNet.Security.Google;
using Microsoft.AspNet.Security.MicrosoftAccount;
using Microsoft.AspNet.Security.Twitter;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;
using Microsoft.Framework.Runtime;
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
        public StartupSocialTesting(IApplicationEnvironment appEnvironment)
        {
            //Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
            //then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            Configuration = new Configuration()
                        .AddJsonFile("config.json")
                        .AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.

            // Used to override some configuration parameters that cannot be overridden by environment.
            if (File.Exists(Path.Combine(appEnvironment.ApplicationBasePath, "configoverride.json")))
            {
                ((Configuration)Configuration).AddJsonFile("configoverride.json");
            }
        }

        public IConfiguration Configuration { get; private set; }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            //Error page middleware displays a nice formatted HTML page for any unhandled exceptions in the request pipeline.
            //Note: ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production.
            app.UseErrorPage(ErrorPageOptions.ShowAll);

            app.UseDatabaseErrorPage(DatabaseErrorPageOptions.ShowAll);

            app.UseServices(services =>
            {
                //Sql client not available on mono
                string value;
                var useInMemoryStore = Configuration.TryGet("UseInMemoryStore", out value) && value == "true" ?
                    true :
                    Type.GetType("Mono.Runtime") != null;

                // Add EF services to the services container
                if (useInMemoryStore)
                {
                    services.AddEntityFramework(Configuration)
                            .AddInMemoryStore()
                            .AddDbContext<MusicStoreContext>();
                }
                else
                {
                    services.AddEntityFramework(Configuration)
                            .AddSqlServer()
                            .AddDbContext<MusicStoreContext>();
                }

                // Add Identity services to the services container
                services.AddIdentity<ApplicationUser, IdentityRole>(Configuration)
                        .AddEntityFrameworkStores<MusicStoreContext>()
                        .AddDefaultTokenProviders()
                        .AddMessageProvider<EmailMessageProvider>()
                        .AddMessageProvider<SmsMessageProvider>();

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
                services.AddSingleton<IMemoryCache, MemoryCache>();

                // Configure Auth
                services.Configure<AuthorizationOptions>(options =>
                {
                    options.AddPolicy("ManageStore", new AuthorizationPolicyBuilder().RequiresClaim("ManageStore", "Allowed").Build());
                });
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