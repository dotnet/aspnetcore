// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ErrorPageMiddlewareWebSite;

public class Startup
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews()
            .AddRazorRuntimeCompilation();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
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
}

