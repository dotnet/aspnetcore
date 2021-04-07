// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HostFilteringSample
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            return BuildWebHost(args).RunAsync();
        }

        public static IHost BuildWebHost(string[] args)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .ConfigureLogging((_, factory) =>
                    {
                        factory.SetMinimumLevel(LogLevel.Debug);
                        factory.AddConsole();
                    })
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        var env = hostingContext.HostingEnvironment;
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                              .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    })
                    .UseKestrel()
                    .UseStartup<Startup>();
                });

            return hostBuilder.Build();
        }
    }
}
