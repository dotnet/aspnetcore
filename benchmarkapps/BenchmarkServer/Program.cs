// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BenchmarkServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Process ID: {Process.GetCurrentProcess().Id}");

            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddCommandLine(args)
                .Build();

            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .ConfigureLogging(loggerFactory =>
                {
                    if (Enum.TryParse(config["LogLevel"], out LogLevel logLevel))
                    {
                        loggerFactory.AddConsole().SetMinimumLevel(logLevel);
                    }
                })
                .UseKestrel()
                .UseStartup<Startup>();

            host.Build().Run();
        }
    }
}
