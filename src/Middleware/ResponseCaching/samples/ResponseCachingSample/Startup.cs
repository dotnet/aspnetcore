// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                MaxAge = TimeSpan.FromSeconds(10)
            };
            context.Response.Headers.Vary = new string[] { "Accept-Encoding" };

            await context.Response.WriteAsync("Hello World! " + DateTime.UtcNow);
        });
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
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
