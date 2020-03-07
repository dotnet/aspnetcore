// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace RoutingSandbox
{
    public class Program
    {
        public const string EndpointRoutingScenario = "endpointrouting";
        public const string RouterScenario = "router";

        public static void Main(string[] args)
        {
            var webHost = GetWebHostBuilder(args).Build();
            webHost.Run();
        }

        // For unit testing
        public static IWebHostBuilder GetWebHostBuilder(string[] args)
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

            return new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration()
                .ConfigureLogging(b =>
                {
                    b.AddConsole();
                    b.SetMinimumLevel(LogLevel.Critical);
                })
                .UseContentRoot(Environment.CurrentDirectory)
                .UseStartup(startupType);
        }
    }
}
