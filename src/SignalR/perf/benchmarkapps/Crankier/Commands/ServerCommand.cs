// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.CommandLineUtils;
using static Microsoft.AspNetCore.SignalR.Crankier.Commands.CommandLineUtilities;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Crankier.Server;

namespace Microsoft.AspNetCore.SignalR.Crankier.Commands
{
    internal class ServerCommand
    {
        public static void Register(CommandLineApplication app, params string[] args)
        {
            app.Command("server", cmd =>
            {
                cmd.OnExecute(() =>
                {
                    return Execute();
                });
            });
        }

        private static int Execute(params string[] args)
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

            return 0;
        }
    }
}
