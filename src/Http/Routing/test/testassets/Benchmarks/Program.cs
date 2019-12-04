// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            GetWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder GetWebHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "RoutingBenchmarks_")
                .Build();

            // Consoler logger has a major impact on perf results, so do not use
            // default builder.

            var webHostBuilder = new WebHostBuilder()
                    .UseConfiguration(config)
                    .UseKestrel();

            var scenario = config["scenarios"]?.ToLower();
            if (scenario == "plaintextdispatcher" || scenario == "plaintextendpointrouting")
            {
                webHostBuilder.UseStartup<StartupUsingEndpointRouting>();
                // for testing
                webHostBuilder.UseSetting("Startup", nameof(StartupUsingEndpointRouting));
            }
            else if (scenario == "plaintextrouting" || scenario == "plaintextrouter")
            {
                webHostBuilder.UseStartup<StartupUsingRouter>();
                // for testing
                webHostBuilder.UseSetting("Startup", nameof(StartupUsingRouter));
            }
            else
            {
                throw new InvalidOperationException(
                    $"Invalid scenario '{scenario}'. Allowed scenarios are PlaintextEndpointRouting and PlaintextRouter");
            }

            return webHostBuilder;
        }
    }
}
