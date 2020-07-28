// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BasicWebSite
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                        .UseStartup<StartupWithoutEndpointRouting>()
                        .UseKestrel()
                        .UseIISIntegration();
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            return host.RunAsync();
        }
    }

    public class TestService
    {
        public string Message { get; set; }
    }
}
