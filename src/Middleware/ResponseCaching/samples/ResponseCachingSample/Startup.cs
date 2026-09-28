// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Net.Http.Headers;

namespace ResponseCachingSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddResponseCaching();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseResponseCaching();
        app.Run(async (context) =>
        {
            context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(60)
            };

            var responseCachingFeature = context.Features.Get<IResponseCachingFeature>();
            if (responseCachingFeature != null)
            {
                responseCachingFeature.VaryByQueryKeys = [ "*" ];
            }

            var user = context.Request.Query["user"].FirstOrDefault() ?? "(none)";
            var theme = context.Request.Query["theme"].FirstOrDefault() ?? "(none)";

            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync($"User: {user} | Theme: {theme} | Generated: {DateTime.UtcNow:O}");
        });
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();
            }).Build();

        return host.RunAsync();
    }
}
