// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Dnx.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class Program
    {
        private const string HostingIniFile = "Microsoft.AspNet.Hosting.ini";
        private const string ConfigFileKey = "config";

        private readonly IServiceProvider _serviceProvider;

        public Program(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Main(string[] args)
        {
            // Allow the location of the ini file to be specified via a --config command line arg
            var tempBuilder = new ConfigurationBuilder().AddCommandLine(args);
            var tempConfig = tempBuilder.Build();
            var configFilePath = tempConfig[ConfigFileKey] ?? HostingIniFile;

            var appBasePath = _serviceProvider.GetRequiredService<IApplicationEnvironment>().ApplicationBasePath;
            var builder = new ConfigurationBuilder(appBasePath);
            builder.AddIniFile(configFilePath, optional: true);
            builder.AddEnvironmentVariables();
            builder.AddCommandLine(args);
            var config = builder.Build();

            var host = new WebHostBuilder(_serviceProvider, config).Build();
            using (host.Start())
            {
                Console.WriteLine("Started");
                var appShutdownService = host.ApplicationServices.GetRequiredService<IApplicationShutdown>();
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    appShutdownService.RequestShutdown();
                    // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
                    eventArgs.Cancel = true;
                };
                appShutdownService.ShutdownRequested.WaitHandle.WaitOne();
            }
        }
    }
}
