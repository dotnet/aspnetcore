// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Features;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public class Program
    {
        private const string HostingJsonFile = "Microsoft.AspNet.Hosting.json";
        private const string ConfigFileKey = "config";

        private readonly IServiceProvider _serviceProvider;

        public Program(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Main(string[] args)
        {
            // Allow the location of the json file to be specified via a --config command line arg
            var tempBuilder = new ConfigurationBuilder().AddCommandLine(args);
            var tempConfig = tempBuilder.Build();
            var configFilePath = tempConfig[ConfigFileKey] ?? HostingJsonFile;

            var appBasePath = _serviceProvider.GetRequiredService<IApplicationEnvironment>().ApplicationBasePath;
            var config = new ConfigurationBuilder()
                .SetBasePath(appBasePath)
                .AddJsonFile(configFilePath, optional: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var host = new WebHostBuilder(_serviceProvider, config, captureStartupErrors: true).Build();
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
