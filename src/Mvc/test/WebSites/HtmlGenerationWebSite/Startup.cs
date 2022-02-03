// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.Caching.Memory;

namespace HtmlGenerationWebSite;

public class Startup
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        // Add MVC services to the services container. Change default FormTagHelper.AntiForgery to false. Usually
        // null which is interpreted as true unless element includes an action attribute.
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
            endpoints.MapControllerRoute(
                name: "areaRoute",
                pattern: "{area:exists}/{controller}/{action}/{id?}",
                defaults: new { action = "Index" });
            endpoints.MapControllerRoute(
                name: "productRoute",
                pattern: "Product/{action}",
                defaults: new { controller = "Product" });
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller}/{action}/{id?}",
                defaults: new { controller = "HtmlGeneration_Home", action = "Index" });

            endpoints.MapRazorPages();
        });
    }

    public static void Main(string[] args)
    {
        var host = CreateWebHostBuilder(args)
            .Build();

        host.Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        new WebHostBuilder()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseStartup<Startup>()
            .UseKestrel()
            .UseIISIntegration();

    protected virtual void ConfigureMvcOptions(MvcOptions options)
    {
    }
}
