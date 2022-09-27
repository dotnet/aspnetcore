// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.TestHost;

namespace RoutingWebSite;

public class Program
{
    public const string EndpointRoutingScenario = "endpointrouting";
    public const string RouterScenario = "router";

    public static Task Main(string[] args)
    {
        var host = GetHostBuilder(args).Build();
        return host.RunAsync();
    }

    // For unit testing
    public static IHostBuilder GetHostBuilder(string[] args)
    {
        string scenario;
        if (args.Length == 0)
        {
            Console.WriteLine("Choose a sample to run:");
            Console.WriteLine($"1. {EndpointRoutingScenario}");
            Console.WriteLine($"2. {RouterScenario}");
            Console.WriteLine();

            scenario = Console.ReadLine();
        }
        else
        {
            scenario = args[0];
        }

        Type startupType;
        switch (scenario)
        {
            case "1":
            case EndpointRoutingScenario:
                startupType = typeof(UseEndpointRoutingStartup);
                break;

            case "2":
            case RouterScenario:
                startupType = typeof(UseRouterStartup);
                break;

            default:
                Console.WriteLine($"unknown scenario {scenario}");
                Console.WriteLine($"usage: dotnet run -- ({EndpointRoutingScenario}|{RouterScenario})");
                throw new InvalidOperationException();

        }

        return new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .UseIISIntegration()
                    .UseContentRoot(Environment.CurrentDirectory)
                    .UseStartup(startupType)
                    .UseTestServer();
            })
            .ConfigureLogging(b =>
            {
                b.AddConsole();
                b.SetMinimumLevel(LogLevel.Critical);
            });
    }
}
