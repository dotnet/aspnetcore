using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Security;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using MusicStore.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

public class Startup
{
    public void Configure(IBuilder app)
    {
        app.UseServices(services =>
        {
            /* Adding IConfiguration as a service in the IoC to avoid instantiating Configuration again.
             * Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
             * then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            */
            var configuration = new Configuration();
            configuration.AddJsonFile("LocalConfig.json");
            configuration.AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.
            services.AddInstance<IConfiguration>(configuration);

            //Add all MVC related services to IoC.
            services.AddMvc();

            /*Add all EF related services to IoC.*/
            services.AddEntityFramework().AddSqlServer();
            services.AddTransient<MusicStoreContext>();

            //Add all Identity related services to IoC. 
            services.AddTransient<DbContext, ApplicationDbContext>();
            services.AddIdentity<ApplicationUser, IdentityRole>(s =>
            {
                s.AddEntity();
            });
            services.AddTransient<SignInManager<ApplicationUser>>();
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
        CreateAdminUser(app.ApplicationServices).Wait();
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

        var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
        // Todo: identity sql does not support roles yet
        //var roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();
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