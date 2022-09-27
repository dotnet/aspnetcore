// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace FilesWebSite;

public class Startup
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddNewtonsoftJson();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
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
