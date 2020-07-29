// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SecurityWebSite
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            var host = CreateHostBuilder(args)
                .Build();

            return host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseKestrel()
                        .UseStartup<Startup>();
                })
                .UseContentRoot(Directory.GetCurrentDirectory());
    }
}
