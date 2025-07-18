// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace RazorBuildWebSite;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var fileProvider = new UpdateableFileProvider();
        services.AddSingleton(fileProvider);

        services.AddMvc()
            .AddRazorRuntimeCompilation(options => options.FileProviders.Add(fileProvider));
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

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>()
                    .UseKestrel()
                    .UseIISIntegration();
            });
}
