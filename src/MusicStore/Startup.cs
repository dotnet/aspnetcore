using Microsoft.AspNet;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Entity;
using Microsoft.AspNet.Identity.InMemory;
using Microsoft.AspNet.Identity.Security;
using Microsoft.AspNet.Logging;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.RequestContainer;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Data.Entity;
using Microsoft.Data.InMemory;
using Microsoft.Data.SqlServer;
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
            //Add configuration as a service
            var applicationEnvironment = app.ApplicationServices.GetService<IApplicationEnvironment>();
            var configuration = new Configuration();
            configuration.AddJsonFile(Path.Combine(applicationEnvironment.ApplicationBasePath, "Config.json"));
            configuration.AddEnvironmentVariables(); //If configuration flows through environment we should pick that first
            services.AddInstance<IConfiguration>(configuration);

            services.AddInstance<ILoggerFactory>(new NullLoggerFactory());
            services.AddMvc();
#if NET45
            services.AddEntityFramework(s => s.AddSqlServer());
#else
            services.AddEntityFramework(s => s.AddInMemoryStore());
#endif
            services.AddTransient<MusicStoreContext, MusicStoreContext>();
            // File an issue trying to use IdentityUser/IdentityRole and open generic UserManager<>
            services.AddIdentity<ApplicationUser, IdentityRole>(s =>
            {
                // Turn off password defaults since register error display blows up
                s.UsePasswordValidator(() => new PasswordValidator()); 

                //s.UseDbContext(() => context);
                //s.UseUserStore(() => new UserStore(context));
                s.UseUserStore(() => new InMemoryUserStore<ApplicationUser>());
                s.UseUserManager<ApplicationUserManager>();
                s.UseRoleStore(() => new InMemoryRoleStore<IdentityRole>());
                s.UseRoleManager<ApplicationRoleManager>();
            });
        });

        //ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production. 
        app.UseErrorPage(ErrorPageOptions.ShowAll);

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
                "{controller}/{action}",
                new { controller = "Home", action = "Index" });
        });

        SampleData.InitializeMusicStoreDatabaseAsync(app.ApplicationServices).Wait();
        CreateAdminUser(app.ApplicationServices).Wait();
    }

    private async Task CreateAdminUser(IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetService<IConfiguration>();
        var userName = configuration.Get("DefaultAdminUsername");
        var password = configuration.Get("DefaultAdminPassword");
        const string adminRole = "Administrator";

        var userManager = serviceProvider.GetService<ApplicationUserManager>();
        var roleManager = serviceProvider.GetService<ApplicationRoleManager>();

        if (!await roleManager.RoleExistsAsync(adminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        var user = await userManager.FindByNameAsync(userName);
        if (user == null)
        {
            user = new ApplicationUser { UserName = userName };
            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, adminRole);
            await userManager.AddClaimAsync(user, new Claim("ManageStore", "Allowed"));
        }
    }
}