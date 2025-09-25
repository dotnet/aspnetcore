// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ApiExplorerWebSite.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;

namespace ApiExplorerWebSite;

public class Startup
{
    // Set up application services
    public void ConfigureServices(IServiceCollection services)
    {
        var wellKnownChangeToken = new WellKnownChangeToken();
        services.AddControllers(options =>
        {
            options.Filters.AddService(typeof(ApiExplorerDataFilter));

            options.Conventions.Add(new ApiExplorerVisibilityEnabledConvention());
            options.Conventions.Add(new ApiExplorerVisibilityDisabledConvention(
                typeof(ApiExplorerVisibilityDisabledByConventionController)));
            options.Conventions.Add(new ApiExplorerInboundOutboundConvention(
                typeof(ApiExplorerInboundOutBoundController)));
            options.Conventions.Add(new ApiExplorerRouteChangeConvention(wellKnownChangeToken));

            options.OutputFormatters.Clear();
            options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
        })
        .AddNewtonsoftJson();

        services.AddSingleton<ApiExplorerDataFilter>();
        services.AddSingleton<IActionDescriptorChangeProvider, ActionDescriptorChangeProvider>();
        services.AddSingleton(wellKnownChangeToken);
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
                    .UseKestrel()
                    .UseIISIntegration()
                    .UseStartup<Startup>();
            });
}

