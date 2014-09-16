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

namespace MusicStore
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            //Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
            //then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            var configuration = new Configuration()
                        .AddJsonFile("config.json")
                        .AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.

            //Error page middleware displays a nice formatted HTML page for any unhandled exceptions in the request pipeline.
            //Note: ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production.
            app.UseErrorPage(ErrorPageOptions.ShowAll);

            app.SetDefaultSignInAsAuthenticationType("External");

            app.UseServices(services =>
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
                services.SetupOptions<MusicStoreDbContextOptions>(options =>
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
                services.AddIdentitySqlServer<MusicStoreContext, ApplicationUser>()
                        .AddAuthentication();

                // Add MVC services to the services container
                services.AddMvc();

                //Add all SignalR related services to IoC.
                services.AddSignalR();

                //Add InMemoryCache
                //Currently not able to AddSingleTon
                services.AddInstance<IMemoryCache>(new MemoryCache());
            });

            //Configure SignalR
            app.UseSignalR();

            // Add static files to the request pipeline
            app.UseStaticFiles();

            // Add cookie-based authentication to the request pipeline
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "External",
                AuthenticationMode = AuthenticationMode.Passive,
                ExpireTimeSpan = TimeSpan.FromMinutes(5)
            });

            // Add cookie-based authentication to the request pipeline
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = ClaimsIdentityOptions.DefaultAuthenticationType,
                LoginPath = new PathString("/Account/Login")
            });

            app.UseTwoFactorSignInCookies();

            app.UseFacebookAuthentication(new FacebookAuthenticationOptions()
            {
                AppId = "[AppId]",
                AppSecret = "[AppSecret]",
            });

            app.UseGoogleAuthentication(new GoogleAuthenticationOptions()
            {
                ClientId = "[ClientId]",
                ClientSecret = "[ClientSecret]",
            });

            app.UseTwitterAuthentication(new TwitterAuthenticationOptions()
            {
                ConsumerKey = "[ConsumerKey]",
                ConsumerSecret = "[ConsumerSecret]",
            });

            app.UseMicrosoftAccountAuthentication(new MicrosoftAccountAuthenticationOptions()
            {
                Caption = "MicrosoftAccount - Requires project changes",
                ClientId = "[ClientId]",
                ClientSecret = "[ClientSecret]",
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