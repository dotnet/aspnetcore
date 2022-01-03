// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace CookieSessionSample;

public static class Program
{
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
            })
            .ConfigureLogging(factory =>
            {
                factory.AddConsole();
                factory.AddFilter("Console", level => level >= LogLevel.Information);
            })
            .Build();

        return host.RunAsync();
    }
}
