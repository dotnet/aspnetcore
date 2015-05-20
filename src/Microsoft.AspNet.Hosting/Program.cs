// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Runtime;

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
            // Allow the location of the ini file to be specfied via a --config command line arg
            var tempConfig = new ConfigurationSection().AddCommandLine(args);
            var configFilePath = tempConfig[ConfigFileKey] ?? HostingIniFile;

            var appBasePath = _serviceProvider.GetRequiredService<IApplicationEnvironment>().ApplicationBasePath;
            var config = new ConfigurationSection(appBasePath);
            config.AddIniFile(configFilePath, optional: true);
            config.AddEnvironmentVariables();
            config.AddCommandLine(args);

            var host = new WebHostBuilder(_serviceProvider, config).Build();
            using (host.Start())
            {
                Console.WriteLine("Started");
                var appShutdownService = host.ApplicationServices.GetRequiredService<IApplicationShutdown>();
                Console.CancelKeyPress += delegate { appShutdownService.RequestShutdown(); };
                appShutdownService.ShutdownRequested.WaitHandle.WaitOne();
            }
        }
    }
}
