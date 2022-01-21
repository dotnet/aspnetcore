// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore;

namespace SampleApp;

public class Program
{
    public static void Main(string[] args)
    {
        var app = WebApplication.Create(args);

        app.UseStaticFiles();
        app.MapGet("/", (Func<string>)(() => "Hello, World!"));

        app.Run();
    }

    private static void HelloWorld()
    {
        using (WebHost.Start(context => context.Response.WriteAsync("Hello, World!")))
        {
            //host.WaitForShutdown(); // TODO: https://github.com/aspnet/Hosting/issues/1022
            Console.WriteLine("Running HelloWorld: Press any key to shutdown and start the next sample...");
            Console.ReadKey();
        }
    }

    private static void CustomUrl()
    {
        // Changing the listening URL
        using (WebHost.Start("http://localhost:8080", context => context.Response.WriteAsync("Hello, World!")))
        {
            //host.WaitForShutdown(); // TODO: https://github.com/aspnet/Hosting/issues/1022
            Console.WriteLine("Running CustomUrl: Press any key to shutdown and start the next sample...");
            Console.ReadKey();
        }
    }

    private static void CustomRouter()
    {
        // Using a router
        using (WebHost.Start(router => router
            .MapGet("hello/{name}", (req, res, data) => res.WriteAsync($"Hello, {data.Values["name"]}"))
            .MapGet("goodbye/{name}", (req, res, data) => res.WriteAsync($"Goodbye, {data.Values["name"]}"))
            .MapGet("throw/{message?}", (req, res, data) => throw new Exception((string)data.Values["message"] ?? "Uh oh!"))
            .MapGet("{greeting}/{name}", (req, res, data) => res.WriteAsync($"{data.Values["greeting"]}, {data.Values["name"]}"))
            .MapGet("", (req, res, data) => res.WriteAsync($"Hello, World!"))))
        {
            //host.WaitForShutdown(); // TODO: https://github.com/aspnet/Hosting/issues/1022
            Console.WriteLine("Running CustomRouter: Press any key to shutdown and start the next sample...");
            Console.ReadKey();
        }
    }

    private static void CustomApplicationBuilder()
    {
        // Using a application builder
        using (WebHost.StartWith(app =>
        {
            app.UseStaticFiles();
            app.Run(async context =>
            {
                await context.Response.WriteAsync("Hello, World!");
            });
        }))
        {
            //host.WaitForShutdown(); // TODO: https://github.com/aspnet/Hosting/issues/1022
            Console.WriteLine("Running CustomApplicationBuilder: Press any key to shutdown and start the next sample...");
            Console.ReadKey();
        }
    }
    private static void DirectWebHost(string[] args)
    {
        // Using defaults with a Startup class
        using (var host = WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .Build())
        {
            host.Run();
        }
    }

    private static void HostBuilderWithWebHost(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddCommandLine(args);
            })
            .ConfigureWebHostDefaults(builder =>
            {
                builder.UseStartup<Startup>();
            })
            .Build();

        host.Run();
    }

    private static void DefaultGenericHost(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
