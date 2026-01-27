// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace HtmlGenerationWebSite;

public class StartupWithStaticAssets
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(ConfigureMvcOptions)
            .InitializeTagHelper<FormTagHelper>((helper, _) => helper.Antiforgery = false);

        services.AddSingleton(typeof(ISignalTokenProviderService<>), typeof(SignalTokenProviderService<>));
        services.AddSingleton<ProductsService>();
        services.Configure<MemoryCacheOptions>(o => o.TrackLinkedCacheEntries = true);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseStaticFiles();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            // Test that WithStaticAssets() can be called on the builder returned by
            // MapControllerRoute() without errors. This validates the fix for the issue
            // where MapControllerRoute() returned a builder that didn't have the
            // EndpointRouteBuilderKey set in its Items dictionary.
            //
            // Note: Without a MapStaticAssets() call, WithStaticAssets() will add empty
            // metadata, but it should not throw.

            endpoints.MapControllerRoute(
                name: "areaRoute",
                pattern: "{area:exists}/{controller}/{action}/{id?}",
                defaults: new { action = "Index" })
                .WithStaticAssets();

            endpoints.MapControllerRoute(
                name: "productRoute",
                pattern: "Product/{action}",
                defaults: new { controller = "Product" })
                .WithStaticAssets();

            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action}/{id?}",
                defaults: new { controller = "HtmlGeneration_Home", action = "Index" })
                .WithStaticAssets();

            endpoints.MapRazorPages();
        });
    }

    protected virtual void ConfigureMvcOptions(MvcOptions options)
    {
    }
}
