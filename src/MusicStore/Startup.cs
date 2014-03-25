// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.AspNet.DependencyInjection.NestedProviders;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.InMemory;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using MusicStore.Models;
using MusicStore.Web.Models;
using System;
using System.Collections.Generic;

public class Startup
{
    public void Configuration(IBuilder app)
    {
        CreateAdminUser();

        app.UseFileServer();

        var serviceProvider = MvcServices.GetDefaultServices().BuildServiceProvider(app.ServiceProvider);

        var routes = new RouteCollection()
        {
            DefaultHandler = new MvcApplication(serviceProvider),
        };

        routes.MapRoute(
            "{controller}/{action}",
            new { controller = "Home", action = "Index" });

        routes.MapRoute(
            "{controller}",
            new { controller = "Home" });

        app.UseRouter(routes);

        SampleData.InitializeMusicStoreDatabase();
    }

    //Bug: We need EF to integrate with SQL server. Until then we will use in memory store
    //private async void CreateAdminUser()
    //{
    //    string _username = ConfigurationManager.AppSettings["DefaultAdminUsername"];
    //    string _password = ConfigurationManager.AppSettings["DefaultAdminPassword"];
    //    string _role = "Administrator";

    //    var context = new ApplicationDbContext();
    //    var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
    //    var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));

    //    var role = new IdentityRole(_role);
    //    var result = await roleManager.RoleExistsAsync(_role);
    //    if (result == false)
    //    {
    //        await roleManager.CreateAsync(role);
    //    }

    //    var user = await userManager.FindByNameAsync(_username);
    //    if (user == null)
    //    {
    //        user = new ApplicationUser { UserName = _username };
    //        await userManager.CreateAsync(user, _password);
    //        await userManager.AddToRoleAsync(user.Id, _role);
    //    }
    //}

    private async void CreateAdminUser()
    {
        //How to read from local appSettings? 
        var configuration = new Configuration();
        string _username = "Administrator"; // configuration.Get("DefaultAdminUsername");
        string _password = "YouShouldChangeThisPassword"; // configuration.Get("DefaultAdminPassword");
        string _role = "Administrator";

        var userManager = new UserManager<ApplicationUser, string>(new InMemoryUserStore<ApplicationUser>());
        var roleManager = new RoleManager<InMemoryRole>(new InMemoryRoleStore());

        var role = new InMemoryRole(_role);
        var result = await roleManager.RoleExists(_role);
        if (result == false)
        {
            await roleManager.Create(role);
        }

        var user = await userManager.FindByName(_username);
        if (user == null)
        {
            user = new ApplicationUser { UserName = _username };
            await userManager.Create(user, _password);
            await userManager.AddToRole(user.Id, _role);
        }
    }
}