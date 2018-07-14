// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace IISTestSite
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var envVariable = Environment.GetEnvironmentVariable("ASPNETCORE_INPROCESS_INITIAL_WRITE");
            if (!string.IsNullOrEmpty(envVariable))
            {
                Console.WriteLine(envVariable);
                Console.Error.WriteLine(envVariable);
            }

            var host = new WebHostBuilder()
                .ConfigureLogging((_, factory) =>
                {
                    factory.AddConsole();
                    factory.AddFilter("Console", level => level >= LogLevel.Information);
                })
                .UseIIS()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
