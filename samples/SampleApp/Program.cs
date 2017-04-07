using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SampleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //HelloWorld();

            //CustomUrl();

            Router();

            //StartupClass(args);
        }

        private static void HelloWorld()
        {
            var host = WebHost.Start(context => context.Response.WriteAsync("Hello, World!"));
            //host.WaitForShutdown(); // TODO: This method needs to be added to Hosting
            Console.WriteLine("Press any key to shutdown...");
            Console.ReadKey();
        }

        private static void CustomUrl()
        {
            // Changing the listening URL
            var host = WebHost.Start("http://localhost:8080", context => context.Response.WriteAsync("Hello, World!"));
            //host.WaitForShutdown(); // TODO: This method needs to be added to Hosting
            Console.WriteLine("Press any key to shutdown...");
            Console.ReadKey();
        }

        private static void Router()
        {
            // Using a router
            var host = WebHost.Start(router => router
                .MapGet("hello/{name}", (req, res, data) => res.WriteAsync($"Hello, {data.Values["name"]}"))
                .MapGet("goodbye/{name}", (req, res, data) => res.WriteAsync($"Goodbye, {data.Values["name"]}"))
                .MapGet("throw/{message?}", (req, res, data) => throw new Exception((string)data.Values["message"] ?? "Uh oh!"))
                .MapGet("{greeting}/{name}", (req, res, data) => res.WriteAsync($"{data.Values["greeting"]}, {data.Values["name"]}"))
                .MapGet("", (req, res, data) => res.WriteAsync($"Hello, World!"))
            );
            //host.WaitForShutdown(); // TODO: This method needs to be added to Hosting
            Console.WriteLine("Press any key to shutdown...");
            Console.ReadKey();
        }

        private static void StartupClass(string[] args)
        {
            // Using defaults with a Startup class
            var host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
            host.Run();
        }
    }
}
