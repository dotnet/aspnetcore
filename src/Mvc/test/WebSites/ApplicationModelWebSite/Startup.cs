// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace ApplicationModelWebSite;

public class Startup
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.Conventions.Add(new ApplicationDescription("Common Application Description"));
            options.Conventions.Add(new ControllerLicenseConvention());
            options.Conventions.Add(new FromHeaderConvention());
            options.Conventions.Add(new MultipleAreasControllerConvention());
            options.Conventions.Add(new CloneActionConvention());
        });

        services.AddRazorPages();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(name: "areaRoute", pattern: "{area:exists}/{controller=Home}/{action=Index}");
            endpoints.MapControllerRoute(name: "default", pattern: "{controller}/{action}/{id?}");

            endpoints.MapRazorPages();
        });
    }

    public static void Main(string[] args)
    {
        using var host = CreateHostBuilder(args)
            .Build();

        host.Run();
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

