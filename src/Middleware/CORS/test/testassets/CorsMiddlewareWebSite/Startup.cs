// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CorsMiddlewareWebSite;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseCors(policy => policy.WithOrigins("http://example.com"));
        app.UseMiddleware<EchoMiddleware>();
    }
    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>();
            })
            .Build();

        return host.RunAsync();
    }
}
