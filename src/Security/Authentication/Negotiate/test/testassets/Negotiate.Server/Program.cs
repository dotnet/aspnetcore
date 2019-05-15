// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Negotiate.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using var host1 = CreateHostBuilder(args.Append("Persist=true").ToArray()).Build();
            using var host2 = CreateHostBuilder(args.Append("Persist=false").ToArray()).Build();
            await host1.StartAsync();
            await host2.StartAsync();
            await host1.WaitForShutdownAsync(); // CTL+C
            await host2.StopAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureKestrel((context, options) =>
                    {
                        if (string.Equals("true", context.Configuration["Persist"]))
                        {
                            options.ListenAnyIP(5000);
                            options.ListenAnyIP(5001, listenOptions => listenOptions.UseHttps());
                        }
                        else
                        {
                            options.ListenAnyIP(5002);
                            options.ListenAnyIP(5003, listenOptions => listenOptions.UseHttps());
                        }
                    });
                });
    }
}
