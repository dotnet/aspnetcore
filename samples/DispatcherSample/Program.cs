// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace DispatcherSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseIISIntegration()
                .UseKestrel()
                .UseStartup<Startup>()
                .ConfigureLogging((c, b) => b.AddProvider(new ConsoleLoggerProvider((category, level) => true, includeScopes: false)))
                .Build();

            host.Run();
        }
    }
}
