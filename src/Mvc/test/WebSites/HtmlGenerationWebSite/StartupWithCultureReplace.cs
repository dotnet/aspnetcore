// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Hosting;

namespace HtmlGenerationWebSite;

public class StartupWithCultureReplace
{
    private readonly Startup Startup = new Startup();

    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLocalization();
        Startup.ConfigureServices(services);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRequestLocalization(options =>
        {
            options.SupportedCultures.Add(new CultureInfo("fr-FR"));
            options.SupportedCultures.Add(new CultureInfo("en-GB"));

            options.SupportedUICultures.Add(new CultureInfo("fr-FR"));
            options.SupportedUICultures.Add(new CultureInfo("fr-CA"));
            options.SupportedUICultures.Add(new CultureInfo("en-GB"));
        });

        Startup.Configure(app);
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<StartupWithCultureReplace>()
                    .UseKestrel()
                    .UseIISIntegration();
            });
}
