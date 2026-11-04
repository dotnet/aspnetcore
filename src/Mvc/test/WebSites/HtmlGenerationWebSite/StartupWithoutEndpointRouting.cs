// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace HtmlGenerationWebSite;

public class StartupWithoutEndpointRouting : Startup
{
    public override void Configure(IApplicationBuilder app)
    {
        app.UseStaticFiles();

        app.UseMvc(routes =>
        {
            routes.MapRoute(
                name: "areaRoute",
                template: "{area:exists}/{controller}/{action}/{id?}",
                defaults: new { action = "Index" });
            routes.MapRoute(
                name: "productRoute",
                template: "Product/{action}",
                defaults: new { controller = "Product" });
            routes.MapRoute(
                name: "default",
                template: "{controller}/{action}/{id?}",
                defaults: new { controller = "HtmlGeneration_Home", action = "Index" });
        });
    }

    protected override void ConfigureMvcOptions(MvcOptions options)
    {
        options.EnableEndpointRouting = false;
    }
}
