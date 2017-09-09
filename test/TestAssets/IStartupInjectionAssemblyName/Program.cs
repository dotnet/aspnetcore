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
            var server = new TestServer(CreateWebHostBuilder(args));

            // Mimic application startup messages so application deployer knows that the application has started
            Console.WriteLine("Application started. Press Ctrl+C to shut down.");
            Console.WriteLine("Now listening on: http://localhost:5000");

            Task.Run(async () => Console.WriteLine(await server.CreateClient().GetStringAsync(""))).GetAwaiter().GetResult();
        }

        // Do not change the signature of this method. It's used for tests.
        private static IWebHostBuilder CreateWebHostBuilder(string [] args) =>
            new WebHostBuilder().ConfigureServices(services => services.AddSingleton<IStartup, Startup>());
    }
}
