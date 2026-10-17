// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace HtmlGenerationWebSite;

public class StartupWithStaticAssets(IConfiguration configuration) : Startup
{
    public override void Configure(IApplicationBuilder app)
    {
        app.UseStaticFiles();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets("TestManifests/StaticAssets.endpoints.json");

            if (configuration.GetValue<bool>("UseDefaultControllerRoute"))
            {
                endpoints.MapDefaultControllerRoute()
                    .WithStaticAssets("TestManifests/StaticAssets.endpoints.json");
            }
            else
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action}/{id?}",
                    defaults: new { controller = "HtmlGeneration_Home", action = "Index" })
                    .WithStaticAssets("TestManifests/StaticAssets.endpoints.json");
            }
        });
    }
}
