// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace LocalizationWebsite;

public static class Program
{
    public static Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();

        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                    factory.AddFilter("Console", level => level >= LogLevel.Warning);
                })
                .UseKestrel()
                .UseConfiguration(config)
                .UseStartup("LocalizationWebsite");
            })
            .Build();

        return host.RunAsync();
    }
}
