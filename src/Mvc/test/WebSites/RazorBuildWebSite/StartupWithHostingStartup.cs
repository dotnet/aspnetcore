// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace RazorBuildWebSite;

public class StartupWithHostingStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var fileProvider = new UpdateableFileProvider();

        // RuntimeCompilation supports a hosting startup that adds services before AddRazorPagesServices is invoked. This startup simulates
        // this configuration by simply putting the call to AddRazorRuntimeCompilation ahead of AddControllersWithViews / AddRazorPages.
        var mvcBuilder = new MockMvcBuilder { Services = services, };
        mvcBuilder.AddRazorRuntimeCompilation(options => options.FileProviders.Add(fileProvider));

        services.AddSingleton(fileProvider);
        services.AddControllersWithViews();
        services.AddRazorPages();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapRazorPages();
            endpoints.MapFallbackToPage("/Fallback");
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
        .UseKestrel();

    private class MockMvcBuilder : IMvcBuilder
    {
        public IServiceCollection Services { get; set; }
        public ApplicationPartManager PartManager { get; }
    }
}
