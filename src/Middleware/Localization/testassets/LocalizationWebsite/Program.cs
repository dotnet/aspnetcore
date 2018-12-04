// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LocalizationWebsite
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var host = new WebHostBuilder()
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                    factory.AddFilter("Console", level => level >= LogLevel.Warning);
                })
                .UseKestrel()
                .UseConfiguration(config)
                .UseStartup("LocalizationWebsite")
                .Build();

            host.Run();
        }
    }
}
