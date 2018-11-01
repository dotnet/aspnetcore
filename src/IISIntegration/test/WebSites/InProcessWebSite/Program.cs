// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
                case "CheckLargeStdOutWrites":
                    Console.WriteLine(new string('a', 30000));
                    break;
                case "CheckLargeStdErrWrites":
                    Console.Error.WriteLine(new string('a', 30000));
                    Console.Error.Flush();
                    break;
                case "ConsoleWrite":
                    Console.WriteLine($"Random number: {args[1]}");
                    break;
                case "ConsoleErrorWrite":
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
                case "EarlyReturn":
                    return 12;
                case "HangOnStop":
                    {
                        var host = new WebHostBuilder()
                            .UseIIS()
                            .UseStartup<Startup>()
                            .Build();
                        host.Run();

                        Thread.Sleep(Timeout.Infinite);
                    }
                    break;
                case "CheckConsoleFunctions":
                    // Call a bunch of console functions and make sure none return invalid handle.
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.Title = "Test";
                    Console.WriteLine($"Is Console redirection: {Console.IsOutputRedirected}");
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.WriteLine("彡⾔");
                    break;
                case "OverriddenServer":
                    {
                        var host = new WebHostBuilder()
                             .UseIIS()
                             .ConfigureServices(services => services.AddSingleton<IServer, DummyServer>())
                             .Configure(builder => builder.Run(async context => { await context.Response.WriteAsync("I shouldn't work"); }))
                             .Build();
                        host.Run();
                    }
                    break;
                case "ConsoleErrorWriteStartServer":
                    Console.Error.WriteLine("TEST MESSAGE");
                    return StartServer();
                case "ConsoleWriteStartServer":
                    Console.WriteLine("TEST MESSAGE");
                    return StartServer();
                default:
                    return StartServer();

            }
            return 12;
        }

        private static int StartServer()
        {
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
            return 0;
        }
    }
}
