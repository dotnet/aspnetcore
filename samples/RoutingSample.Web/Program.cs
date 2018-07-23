// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace RoutingSample.Web
{
    public class Program
    {
        public static readonly string GlobalRoutingScenario = "globalrouting";
        public static readonly string RouterScenario = "router";

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
                Console.WriteLine($"1. {GlobalRoutingScenario}");
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
                case "globalrouting":
                    startupType = typeof(UseGlobalRoutingStartup);
                    break;

                case "2":
                case "router":
                    startupType = typeof(UseRouterStartup);
                    break;

                default:
                    Console.WriteLine($"unknown scenario {scenario}");
                    Console.WriteLine($"usage: dotnet run -- ({GlobalRoutingScenario}|{RouterScenario})");
                    throw new InvalidOperationException();

            }

            return new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup(startupType);
        }
    }
}
