// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace HtmlGenerationWebSite;

public class StartupWithStaticAssets
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        // Add MVC services to the services container
        services.AddMvc(ConfigureMvcOptions)
            .InitializeTagHelper<FormTagHelper>((helper, _) => helper.Antiforgery = false);

        services.AddSingleton(typeof(ISignalTokenProviderService<>), typeof(SignalTokenProviderService<>));
        services.AddSingleton<ProductsService>();
        services.Configure<MemoryCacheOptions>(o => o.TrackLinkedCacheEntries = true);
    }

    public virtual void Configure(IApplicationBuilder app)
    {
        app.UseStaticFiles();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            // Map static assets with a test manifest
            endpoints.MapStaticAssets("TestManifests/StaticAssets.endpoints.json");

            // Use MapControllerRoute with WithStaticAssets to test the fix
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action}/{id?}",
                defaults: new { controller = "HtmlGeneration_Home", action = "Index" })
                .WithStaticAssets("TestManifests/StaticAssets.endpoints.json");
        });
    }

    protected virtual void ConfigureMvcOptions(MvcOptions options)
    {
    }
}
