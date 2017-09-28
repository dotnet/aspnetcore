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

            Task.Run(async () => Console.WriteLine(await server.CreateClient().GetStringAsync(""))).GetAwaiter().GetResult();
        }

        // Do not change the signature of this method. It's used for tests.
        private static IWebHostBuilder CreateWebHostBuilder(string [] args) =>
            new WebHostBuilder().SuppressStatusMessages(true).ConfigureServices(services => services.AddSingleton<IStartup, Startup>());
    }
}
