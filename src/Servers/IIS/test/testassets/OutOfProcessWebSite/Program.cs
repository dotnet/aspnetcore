// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
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
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .UseKestrel()
                .Build();

            host.Run();
            return 0;
        }
    }
}

