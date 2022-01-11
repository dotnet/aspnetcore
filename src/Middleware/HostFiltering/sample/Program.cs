// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace HostFilteringSample;

public class Program
{
    public static Task Main(string[] args)
    {
        return BuildWebHost(args).RunAsync();
    }

    public static IHost BuildWebHost(string[] args)
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .ConfigureLogging((_, factory) =>
                {
                    factory.SetMinimumLevel(LogLevel.Debug);
                    factory.AddConsole();
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                          .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                })
                .UseKestrel()
                .UseStartup<Startup>();
            });

        return hostBuilder.Build();
    }
}
