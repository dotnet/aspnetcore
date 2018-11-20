// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace IStartupInjectionAssemblyName
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var webHost = CreateWebHostBuilder(args).Build();
            var applicationName = webHost.Services.GetRequiredService<IHostingEnvironment>().ApplicationName;
            Console.WriteLine(applicationName);
            Console.ReadKey();
        }

        // Do not change the signature of this method. It's used for tests.
        private static IWebHostBuilder CreateWebHostBuilder(string [] args) =>
            new WebHostBuilder()
            .SuppressStatusMessages(true)
            .ConfigureServices(services => services.AddSingleton<IStartup, Startup>());
    }
}
