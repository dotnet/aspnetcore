// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace IISTestSite
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var mode = args.FirstOrDefault();

            switch (mode)
            {
                // Semicolons are appended to env variables; removing them.
                case "CheckLargeStdOutWrites":
                    Console.WriteLine(new string('a', 30000));
                    break;
                case "CheckLargeStdErrWrites":
                    Console.Error.WriteLine(new string('a', 30000));
                    Console.Error.Flush();
                    break;
                case "CheckLogFile":
                    Console.WriteLine($"Random number: {args[1]}");
                    break;
                case "CheckErrLogFile":
                    Console.Error.WriteLine($"Random number: {args[1]}");
                    Console.Error.Flush();
                    break;
                case "CheckOversizedStdErrWrites":
                    Console.WriteLine(new string('a', 31000));
                    break;
                case "CheckOversizedStdOutWrites":
                    Console.Error.WriteLine(new string('a', 31000));
                    Console.Error.Flush();
                    break;
                case "Hang":
                    Thread.Sleep(Timeout.Infinite);
                    break;
                case "Throw":
                    throw new InvalidOperationException("Program.Main exception");
                case "HangOnStop":
                    var host = new WebHostBuilder()
                        .UseIIS()
                        .UseStartup<Startup>()
                        .Build();
                    host.Run();

                    Thread.Sleep(Timeout.Infinite);
                    break;
                case "CheckConsoleFunctions":
                    // Call a bunch of console functions and make sure none return invalid handle.
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.Title = "Test";
                    Console.WriteLine($"Is Console redirection: {Console.IsOutputRedirected}");
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.WriteLine("彡⾔");
                    break;
            }

            return 12;
        }

        public partial class Startup
        {
            public void Configure(IApplicationBuilder app)
            {
                app.Run(async context => await context.Response.WriteAsync("OK"));
            }
        }
    }
}
