// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace WelcomePageSample;

public class Startup
{
    public void Configure(IApplicationBuilder app)
    {
        app.UseWelcomePage();
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
            }).Build();

        return host.RunAsync();
    }
}

