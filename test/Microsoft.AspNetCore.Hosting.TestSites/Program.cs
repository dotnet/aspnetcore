// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace ServerComparison.TestSites
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
                .UseServer(new NoopServer())
                .UseConfiguration(config)
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                    factory.AddFilter<ConsoleLoggerProvider>(level => level >= LogLevel.Warning);
                })
                .UseStartup("Microsoft.AspNetCore.Hosting.TestSites");

            if (config["STARTMECHANIC"] == "Run")
            {
                var host = builder.Build();

                host.Run();
            }
            else if (config["STARTMECHANIC"] == "WaitForShutdown")
            {
                using (var host = builder.Build())
                {
                    host.Start();

                    // Mimic application startup messages so application deployer knows that the application has started
                    Console.WriteLine("Application started. Press Ctrl+C to shut down.");
                    Console.WriteLine("Now listening on: http://localhost:5000");

                    host.WaitForShutdown();
                }
            }
            else
            {
                throw new InvalidOperationException("Starting mechanic not specified");
            }
        }
    }

    public class NoopServer : IServer
    {
        public void Dispose()
        {
        }

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

