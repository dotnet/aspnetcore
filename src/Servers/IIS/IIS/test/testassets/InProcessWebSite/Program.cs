// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestSite;

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
#pragma warning disable ASPDEPR004 // Type or member is obsolete
#pragma warning disable ASPDEPR008 // Type or member is obsolete
                    var host = new WebHostBuilder()
                        .UseIIS()
                        .UseStartup<Startup>()
                        .Build();
#pragma warning restore ASPDEPR008 // Type or member is obsolete
#pragma warning restore ASPDEPR004 // Type or member is obsolete
                    host.Run();

                    Thread.Sleep(Timeout.Infinite);
                }
                break;
            case "IncreaseShutdownLimit":
                {
#pragma warning disable ASPDEPR004 // Type or member is obsolete
#pragma warning disable ASPDEPR008 // Type or member is obsolete
                    var host = new WebHostBuilder()
                        .UseIIS()
                        .UseShutdownTimeout(TimeSpan.FromSeconds(120))
                        .UseStartup<Startup>()
                        .Build();
#pragma warning restore ASPDEPR008 // Type or member is obsolete
#pragma warning restore ASPDEPR004 // Type or member is obsolete

                    host.Run();
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
            case "ConsoleWriteSingle":
                Console.WriteLine("Wow!");
                return 0;
            case "ConsoleWrite30Kb":
                // Write over 30kb to make sure logs are truncated.
                Console.WriteLine(new string('a', 40000));
                return 0;
            case "OverriddenServer":
                {
#pragma warning disable ASPDEPR004 // Type or member is obsolete
#pragma warning disable ASPDEPR008 // Type or member is obsolete
                    var host = new WebHostBuilder()
                            .UseIIS()
                            .ConfigureServices(services => services.AddSingleton<IServer, DummyServer>())
                            .Configure(builder => builder.Run(async context => { await context.Response.WriteAsync("I shouldn't work"); }))
                            .Build();
#pragma warning restore ASPDEPR008 // Type or member is obsolete
#pragma warning restore ASPDEPR004 // Type or member is obsolete
                    host.Run();
                }
                break;
            case "ConsoleErrorWriteStartServer":
                Console.Error.WriteLine("TEST MESSAGE");
                return StartServer();
            case "ConsoleWriteStartServer":
                Console.WriteLine("TEST MESSAGE");
                return StartServer();
#if !FORWARDCOMPAT
            case "DecreaseRequestLimit":
                {
#pragma warning disable ASPDEPR004 // Type or member is obsolete
#pragma warning disable ASPDEPR008 // Type or member is obsolete
                    var host = new WebHostBuilder()
                        .ConfigureLogging((_, factory) =>
                        {
                            factory.AddConsole();
                            factory.AddFilter("Console", level => level >= LogLevel.Information);
                        })
                        .UseIIS()
                        .ConfigureServices(services =>
                        {
                            services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = 2);
                        })
                        .UseStartup<Startup>()
                        .Build();
#pragma warning restore ASPDEPR008 // Type or member is obsolete
#pragma warning restore ASPDEPR004 // Type or member is obsolete

                    host.Run();
                    break;
                }
#endif
            case "ThrowInStartup":
                {
#pragma warning disable ASPDEPR004 // Type or member is obsolete
#pragma warning disable ASPDEPR008 // Type or member is obsolete
                    var host = new WebHostBuilder()
                                    .ConfigureLogging((_, factory) =>
                                    {
                                        factory.AddConsole();
                                        factory.AddFilter("Console", level => level >= LogLevel.Information);
                                    })
                                    .UseIIS()
                                    .UseStartup<ThrowingStartup>()
                                    .Build();
#pragma warning restore ASPDEPR008 // Type or member is obsolete
#pragma warning restore ASPDEPR004 // Type or member is obsolete

                    host.Run();
                }

                return 0;
#if !FORWARDCOMPAT
            case "ThrowInStartupGenericHost":
                {
                    var host = new HostBuilder().ConfigureWebHost((c) =>
                    {
                        c.ConfigureLogging((_, factory) =>
                        {
                            factory.AddConsole();
                            factory.AddFilter("Console", level => level >= LogLevel.Information);
                        })
                        .UseIIS()
                        .UseStartup<ThrowingStartup>();
                    });

                    host.Build().Run();
                    return 0;
                }
            case "AddLatin1":
                {
                    AppContext.SetSwitch("Microsoft.AspNetCore.Server.IIS.Latin1RequestHeaders", isEnabled: true);
                    var host = new HostBuilder().ConfigureWebHost((c) =>
                    {
                        c.ConfigureLogging((_, factory) =>
                        {
                            factory.AddConsole();
                            factory.AddFilter("Console", level => level >= LogLevel.Information);
                        })
                        .UseIIS()
                        .UseStartup<Startup>();
                    });

                    host.Build().Run();
                    return 0;
                }
#endif
            default:
                return StartServer();

        }
        return 12;
    }

    private static int StartServer()
    {
#pragma warning disable ASPDEPR004 // Type or member is obsolete
#pragma warning disable ASPDEPR008 // Type or member is obsolete
        var host = new WebHostBuilder()
            .ConfigureLogging((_, factory) =>
            {
                factory.AddConsole();
                factory.AddFilter("Console", level => level >= LogLevel.Information);
            })
            .UseKestrel()
            .UseIIS()
            .UseIISIntegration()
            .UseStartup<Startup>()
            .Build();
#pragma warning restore ASPDEPR008 // Type or member is obsolete
#pragma warning restore ASPDEPR004 // Type or member is obsolete

        host.Run();
        return 0;
    }
}
