using System;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Diagnostics.Entity;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security;
using Microsoft.AspNet.Server.WebListener;
using Microsoft.Framework.Cache.Memory;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;
using Microsoft.Net.Http.Server;
using MusicStore.Models;

namespace MusicStore
{
    /// <summary>
    /// To make runtime to load an environment based startup class, specify the environment by the following ways: 
    /// 1. Drop a Microsoft.AspNet.Hosting.ini file in the wwwroot folder
    /// 2. Add a setting in the ini file named 'ASPNET_ENV' with value of the format 'Startup[EnvironmentName]'. For example: To load a Startup class named
    /// 'StartupNtlmAuthentication' the value of the env should be 'NtlmAuthentication' (eg. ASPNET_ENV=NtlmAuthentication). Runtime adds a 'Startup' prefix to this and loads 'StartupNtlmAuthentication'. 
    /// If no environment name is specified the default startup class loaded is 'Startup'. 
    /// Alternative ways to specify environment are:
    /// 1. Set the environment variable named SET ASPNET_ENV=NtlmAuthentication
    /// 2. For selfhost based servers pass in a command line variable named --env with this value. Eg:
    /// "commands": {
    ///    "web": "Microsoft.AspNet.Hosting --server Microsoft.AspNet.Server.WebListener --server.urls http://localhost:5002 --ASPNET_ENV NtlmAuthentication",
    ///  },
    /// </summary>
    public class StartupNtlmAuthentication
    {
        public StartupNtlmAuthentication()
        {
            //Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
            //then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            Configuration = new Configuration()
                        .AddJsonFile("config.json")
                        .AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.
        }

        public IConfiguration Configuration { get; private set; }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            //Set up NTLM authentication for WebListener like below. 
            //For IIS and IISExpress: Use inetmgr to setup NTLM authentication on the application vDir or modify the applicationHost.config to enable NTLM. 
            if ((app.Server as ServerInformation) != null)
            {
                var serverInformation = (ServerInformation)app.Server;
                serverInformation.Listener.AuthenticationManager.AuthenticationTypes = AuthenticationTypes.NTLM;
            }

            app.Use(async (context, next) =>
            {
                //Who will get admin access? For demo sake I'm listing the currently logged on user as the application administrator. But this can be changed to suit the needs.
                var identity = (ClaimsIdentity)context.User.Identity;

                if (identity.GetUserName() == Environment.GetEnvironmentVariable("USERDOMAIN") + "\\" + Environment.GetEnvironmentVariable("USERNAME"))
                {
                    identity.AddClaim(new Claim("ManageStore", "Allowed"));
                }

                await next.Invoke();
            });

            //Error page middleware displays a nice formatted HTML page for any unhandled exceptions in the request pipeline.
            //Note: ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production.
            app.UseErrorPage(ErrorPageOptions.ShowAll);

            app.UseDatabaseErrorPage(DatabaseErrorPageOptions.ShowAll);

            app.UseServices(services =>
            {
                // Add EF services to the services container
                services.AddEntityFramework(Configuration)
                        .AddSqlServer()
                        .AddDbContext<MusicStoreContext>();

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

            //Configure SignalR
            app.UseSignalR();

            // Add static files to the request pipeline
            app.UseStaticFiles();

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
            SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices, false).Wait();
        }
    }
}