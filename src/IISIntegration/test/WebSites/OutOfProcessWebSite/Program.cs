// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestSite
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var mode = args.FirstOrDefault();
            switch (mode)
            {
                case "CreateFile":
                    File.WriteAllText(args[1], "");
                    return StartServer();
            }

            return StartServer();
        }

        private static int StartServer()
        {
            var host = new WebHostBuilder()
                .ConfigureLogging(
                    (_, factory) => {
                        factory.AddConsole();
                        factory.AddFilter("Console", level => level >= LogLevel.Information);
                    })
                .ConfigureServices(services => {
                    var filter = new OriginalServerAddressesFilter();
                    services.AddSingleton(filter);
                    services.AddSingleton<IStartupFilter>(filter);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseKestrel()
                .Build();

            host.Run();
            return 0;
        }
    }

    internal class OriginalServerAddressesFilter: IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder => {
                ServerAddresses = builder.ServerFeatures.Get<IServerAddressesFeature>();
                next(builder);
            };
        }

        public IServerAddressesFeature ServerAddresses { get; set; }
    }
}

