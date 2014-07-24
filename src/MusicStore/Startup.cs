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

namespace MusicStore
{
    public class Startup
    {
        public void Configure(IBuilder app)
        {
            /* Adding IConfiguration as a service in the IoC to avoid instantiating Configuration again.
                 * Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
                 * then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            */
            var configuration = new Configuration();
            configuration.AddJsonFile("LocalConfig.json");
            configuration.AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.

	     /* Error page middleware displays a nice formatted HTML page for any unhandled exceptions in the request pipeline.
             * Note: ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production.
             */
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
                services.AddIdentitySqlServer<MusicStoreContext, ApplicationUser>()
                        .AddAuthentication();

                // Add MVC services to the services container
                services.AddMvc();
            });

            // Add static files to the request pipeline
            app.UseStaticFiles();

            // Add cookie-based authentication to the request pipeline
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = ClaimsIdentityOptions.DefaultAuthenticationType,
                LoginPath = new PathString("/Account/Login"),
            });

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
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