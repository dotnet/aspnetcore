using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Routing;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;
using Microsoft.AspNet.Security.Facebook;
using Microsoft.AspNet.Security.Google;
using Microsoft.AspNet.Security.Twitter;
using Microsoft.AspNet.Security.MicrosoftAccount;
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
                services.AddDefaultIdentity<MusicStoreContext, ApplicationUser, IdentityRole>(configuration);

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
            app.UseIdentity();

            app.UseFacebookAuthentication(new FacebookAuthenticationOptions()
            {
                AppId = "550624398330273",
                AppSecret = "10e56a291d6b618da61b1e0dae3a8954",
            });

            app.UseGoogleAuthentication(new GoogleAuthenticationOptions()
            {
                ClientId = "977382855444.apps.googleusercontent.com",
                ClientSecret = "NafT482F70Vjj_9q1PU4B0pN",
            });

            app.UseTwitterAuthentication(new TwitterAuthenticationOptions()
            {
                ConsumerKey = "9J3j3pSwgbWkgPFH7nAf0Spam",
                ConsumerSecret = "jUBYkQuBFyqp7G3CUB9SW3AfflFr9z3oQBiNvumYy87Al0W4h8",
            });

            //The MicrosoftAccount service has restrictions that prevent the use of http://localhost:5001/ for test applications.
            //As such, here is how to change this sample to uses http://ktesting.com:5001/ instead.

            //Edit the Project.json file and replace http://localhost:5001/ with http://ktesting.com:5001/.

            //From an admin command console first enter:
            // notepad C:\Windows\System32\drivers\etc\hosts
            //and add this to the file, save, and exit (and reboot?):
            // 127.0.0.1 ktesting.com

            //Then you can choose to run the app as admin (see below) or add the following ACL as admin:
            // netsh http add urlacl url=http://ktesting:12345/ user=[domain\user]

            //The sample app can then be run via:
            // k web
            app.UseMicrosoftAccountAuthentication(new MicrosoftAccountAuthenticationOptions()
            {
                Caption = "MicrosoftAccount - Requires project changes",
                ClientId = "000000004012C08A",
                ClientSecret = "GaMQ2hCnqAC6EcDLnXsAeBVIJOLmeutL",
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