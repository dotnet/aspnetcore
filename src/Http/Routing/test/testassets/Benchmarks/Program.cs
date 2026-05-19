// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.TestHost;

namespace Benchmarks;

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
