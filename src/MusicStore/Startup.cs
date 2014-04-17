using Microsoft.AspNet;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Configuration.Json;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.InMemory;
using Microsoft.AspNet.Identity.Security;
using Microsoft.AspNet.Logging;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.RequestContainer;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Net.Runtime;
using MusicStore.Logging;
using MusicStore.Models;
using MusicStore.Web.Models;
using System;
using System.IO;

public class Startup
{

    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddInstance<ILoggerFactory>(new NullLoggerFactory());
        services.AddMvc();

        services.AddInstance<UserManager<ApplicationUser>>(new UserManager<ApplicationUser>(new InMemoryUserStore<ApplicationUser>()));
        services.AddInstance<RoleManager<IdentityRole>>(new RoleManager<IdentityRole>(new InMemoryRoleStore<IdentityRole>()));
    }

    public void Configuration(IBuilder app)
    {
        app.UseContainer(ConfigureServices);

        //ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production. 
        app.UseErrorPage(ErrorPageOptions.ShowAll);

        app.UseFileServer();

        app.UseCookieAuthentication(new CookieAuthenticationOptions()
        {
            AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
            LoginPath = new PathString("/Account/Login")
        });

        var routes = new RouteCollection()
        {
            DefaultHandler = new MvcRouteHandler(),
        };

        routes.MapRoute(
            "{controller}/{action}",
            new { controller = "Home", action = "Index" });

        routes.MapRoute(
            "{controller}",
            new { controller = "Home" });

        app.UseRouter(routes);

        SampleData.InitializeMusicStoreDatabaseAsync().Wait();
        CreateAdminUser(app.ServiceProvider);
    }

    private async void CreateAdminUser(IServiceProvider serviceProvider)
    {
        var applicationEnvironment = serviceProvider.GetService<IApplicationEnvironment>();

        var configuration = new Configuration();
        configuration.AddEnvironmentVariables(); //If configuration flows through environment we should pick that first
        configuration.AddJsonFile(Path.Combine(applicationEnvironment.ApplicationBasePath, "Config.json"));

        string _username = configuration.Get("DefaultAdminUsername");
        string _password = configuration.Get("DefaultAdminPassword");
        string _role = "Administrator";

        var userManager = serviceProvider.GetService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();

        var role = new IdentityRole(_role);
        var result = await roleManager.RoleExistsAsync(_role);
        if (result == false)
        {
            await roleManager.CreateAsync(role);
        }

        var user = await userManager.FindByNameAsync(_username);
        if (user == null)
        {
            user = new ApplicationUser { UserName = _username };
            await userManager.CreateAsync(user, _password);
            await userManager.AddToRoleAsync(user, _role);
        }
    }
}