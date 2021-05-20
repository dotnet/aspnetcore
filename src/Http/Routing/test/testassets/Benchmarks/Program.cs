// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Benchmarks
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            return GetHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder GetHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "RoutingBenchmarks_")
                .Build();

            // Consoler logger has a major impact on perf results, so do not use
            // default builder.

            var hostBuilder = new HostBuilder()
                    .ConfigureWebHost(webHostBuilder =>
                    {
                        webHostBuilder
                            .UseKestrel()
                            .UseTestServer()
                            .UseConfiguration(config);
                    });

            var scenario = config["scenarios"]?.ToLowerInvariant();
            if (scenario == "plaintextdispatcher" || scenario == "plaintextendpointrouting")
            {
                hostBuilder.ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseStartup<StartupUsingEndpointRouting>()
                        // for testing
                        .UseSetting("Startup", nameof(StartupUsingEndpointRouting));
                });
            }
            else if (scenario == "plaintextrouting" || scenario == "plaintextrouter")
            {
                hostBuilder.ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseStartup<StartupUsingRouter>()
                        // for testing
                        .UseSetting("Startup", nameof(StartupUsingRouter));
                });
            }
            else
            {
                throw new InvalidOperationException(
                    $"Invalid scenario '{scenario}'. Allowed scenarios are PlaintextEndpointRouting and PlaintextRouter");
            }

            return hostBuilder;
        }
    }
}
