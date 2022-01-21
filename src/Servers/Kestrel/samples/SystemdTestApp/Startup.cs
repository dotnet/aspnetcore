// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SystemdTestApp;

public class Startup
{
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Default");

        app.Run(async context =>
        {
            var connectionFeature = context.Connection;
            logger.LogDebug($"Peer: {connectionFeature.RemoteIpAddress?.ToString()}:{connectionFeature.RemotePort}"
                + $"{Environment.NewLine}"
                + $"Sock: {connectionFeature.LocalIpAddress?.ToString()}:{connectionFeature.LocalPort}");

            var response = $"hello, world{Environment.NewLine}";
            context.Response.ContentLength = response.Length;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(response);
        });
    }

    public static Task Main(string[] args)
    {
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Console.WriteLine("Unobserved exception: {0}", e.Exception);
        };

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel((context, options) =>
                    {
                        var basePort = context.Configuration.GetValue<int?>("BASE_PORT") ?? 5000;

                        options.Listen(IPAddress.Loopback, basePort, listenOptions =>
                        {
                            // Uncomment the following to enable Nagle's algorithm for this endpoint.
                            //listenOptions.NoDelay = false;

                            listenOptions.UseConnectionLogging();
                        });

                        options.Listen(IPAddress.Loopback, basePort + 1, listenOptions =>
                        {
                            listenOptions.UseHttps();
                            listenOptions.UseConnectionLogging();
                        });

                        options.UseSystemd();

                        // The following section should be used to demo sockets
                        //options.ListenUnixSocket("/tmp/kestrel-test.sock");
                    })
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseStartup<Startup>();
            })
            .ConfigureLogging((_, factory) =>
            {
                factory.AddConsole();
            });

        return hostBuilder.Build().RunAsync();
    }
}
