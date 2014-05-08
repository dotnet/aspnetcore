using Microsoft.AspNet;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Entity;
using Microsoft.AspNet.Identity.InMemory;
using Microsoft.AspNet.Identity.Security;
using Microsoft.Framework.Logging;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.RequestContainer;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.InMemory;
using Microsoft.Data.Entity.SqlServer;
using Microsoft.Net.Runtime;
using MusicStore.Logging;
using MusicStore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

public class Startup
{
    public void Configuration(IBuilder app)
    {
        app.UseServices(services =>
        {
            /* Adding IConfiguration as a service in the IoC to avoid instantiating Configuration again.
             * Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
             * then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            */
            var applicationEnvironment = app.ApplicationServices.GetService<IApplicationEnvironment>();
            var configuration = new Configuration();
            configuration.AddJsonFile(Path.Combine(applicationEnvironment.ApplicationBasePath, "LocalConfig.json"));
            configuration.AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.
            services.AddInstance<IConfiguration>(configuration);

            //Bug: https://github.com/aspnet/Hosting/issues/20
            services.AddInstance<ILoggerFactory>(new NullLoggerFactory());

            //Add all MVC related services to IoC.
            services.AddMvc();

            /*Add all EF related services to IoC.
            Using an InMemoryStore in K until SQL server is available.*/
            services.AddEntityFramework(s => s.AddSqlServer());
            services.AddEntityFramework(s => s.AddInMemoryStore());
            services.AddTransient<MusicStoreContext>();


            /*
             * Add all Identity related services to IoC. 
             * Using an InMemory store to store membership data until SQL server is available. 
             * Users created will be lost on application shutdown.
             */
            services.AddTransient<DbContext, ApplicationDbContext>();

            //Bug: https://github.com/aspnet/Identity/issues/50
            services.AddIdentity<ApplicationUser, IdentityRole>(s =>
            {
                //s.UseDbContext(() => context);
                //s.UseUserStore(() => new UserStore(context));
                s.AddEntity();
                s.AddUserManager<ApplicationUserManager>();
                s.AddRoleManager<ApplicationRoleManager>();
            });
            services.AddTransient<ApplicationSignInManager>();
        });


        /* Error page middleware displays a nice formatted HTML page for any unhandled exceptions in the request pipeline.
         * Note: ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production.
         */
        app.UseErrorPage(ErrorPageOptions.ShowAll);

        //Serves static files in the application.
        app.UseFileServer();

        app.UseCookieAuthentication(new CookieAuthenticationOptions()
        {
            AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
            LoginPath = new PathString("/Account/Login"),
            Notifications = new CookieAuthenticationNotifications
            {
                //OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                //        validateInterval: TimeSpan.FromMinutes(30),
                //        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
            }
        });

        app.UseMvc(routes =>
        {
            routes.MapRoute(
                null,
                "{controller}/{action}",
                new { controller = "Home", action = "Index" });
        });

        //Populates the MusicStore sample data
        SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();
        SampleData.InitializeIdentityDatabaseAsync(app.ApplicationServices).Wait();

        //Creates a Store manager user who can manage the store.
        //CreateAdminUser(app.ApplicationServices).Wait(); // todo: sql identity doesn't support roles yet
    }

    /// <summary>
    /// Creates a store manager user who can manage the inventory.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    private async Task CreateAdminUser(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetService<IConfiguration>();
        var userName = configuration.Get("DefaultAdminUsername");
        var password = configuration.Get("DefaultAdminPassword");
        //const string adminRole = "Administrator";

        var userManager = serviceProvider.GetService<ApplicationUserManager>();
        // Todo: identity sql does not support roles yet
        //var roleManager = serviceProvider.GetService<ApplicationRoleManager>();
        //if (!await roleManager.RoleExistsAsync(adminRole))
        //{
        //    await roleManager.CreateAsync(new IdentityRole(adminRole));
        //}

        var user = await userManager.FindByNameAsync(userName);
        if (user == null)
        {
            user = new ApplicationUser { UserName = userName };
            await userManager.CreateAsync(user, password);
            //await userManager.AddToRoleAsync(user, adminRole);
            await userManager.AddClaimAsync(user, new Claim("ManageStore", "Allowed"));
        }
    }
}