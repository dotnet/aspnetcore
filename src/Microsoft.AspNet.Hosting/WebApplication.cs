// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public class WebApplication
    {
        private const string HostingJsonFile = "hosting.json";
        private const string ConfigFileKey = "config";

        public static void Run(string[] args)
        {
            Run(startupType: null, args: args);
        }

        public static void Run<TStartup>()
        {
            Run(typeof(TStartup), null);
        }

        public static void Run<TStartup>(string[] args)
        {
            Run(typeof(TStartup), args);
        }

        public static void Run(Type startupType)
        {
            Run(startupType, null);
        }

        public static void Run(Type startupType, string[] args)
        {
            // Allow the location of the json file to be specified via a --config command line arg
            var tempBuilder = new ConfigurationBuilder().AddCommandLine(args);
            var tempConfig = tempBuilder.Build();
            var configFilePath = tempConfig[ConfigFileKey] ?? HostingJsonFile;

            var config = new ConfigurationBuilder()
                .AddJsonFile(configFilePath, optional: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var hostBuilder = new WebHostBuilder(config, captureStartupErrors: true);
            if (startupType != null)
            {
                hostBuilder.UseStartup(startupType);
            }
            var host = hostBuilder.Build();
            using (var app = host.Start())
            {
                var hostingEnv = app.Services.GetRequiredService<IHostingEnvironment>();
                Console.WriteLine("Hosting environment: " + hostingEnv.EnvironmentName);

                var serverAddresses = app.ServerFeatures.Get<IServerAddressesFeature>();
                if (serverAddresses != null)
                {
                    foreach (var address in serverAddresses.Addresses)
                    {
                        Console.WriteLine("Now listening on: " + address);
                    }
                }

                Console.WriteLine("Application started. Press Ctrl+C to shut down.");

                var appLifetime = app.Services.GetRequiredService<IApplicationLifetime>();

                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    appLifetime.StopApplication();
                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };

                appLifetime.ApplicationStopping.WaitHandle.WaitOne();
            }
        }
    }
}
