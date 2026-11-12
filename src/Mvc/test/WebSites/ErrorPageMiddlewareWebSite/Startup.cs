// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace ErrorPageMiddlewareWebSite;

public class Startup
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
#pragma warning disable ASPDEPR003 // Type or member is obsolete
        services.AddControllersWithViews()
            .AddRazorRuntimeCompilation();
#pragma warning restore ASPDEPR003 // Type or member is obsolete
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

