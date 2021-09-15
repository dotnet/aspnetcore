// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DelegationSite
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();

            var builder = new WebHostBuilder()
                .UseHttpSys(options =>
                {

                })
                .UseConfiguration(config)
                .SuppressStatusMessages(true)
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                    factory.AddFilter<ConsoleLoggerProvider>(level => level >= LogLevel.Warning);
                })
                .UseStartup<Startup>();

            var host = builder.Build();

            host.Run();
        }
    }
}

