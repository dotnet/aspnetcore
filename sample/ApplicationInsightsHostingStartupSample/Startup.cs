// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IISSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            var logger = loggerFactory.CreateLogger("Requests");

            app.UseMvcWithDefaultRoute();
            app.Map("/log", logApp => logApp.Run(async (context) =>
            {
                TelemetryConfiguration.Active.TelemetryChannel = new CurrentResponseTelemetryChannel(context.Response);

                var systemLogger = loggerFactory.CreateLogger("System.Namespace");
                systemLogger.LogTrace("System trace log");
                systemLogger.LogInformation("System information log");
                systemLogger.LogWarning("System warning log");

                var microsoftLogger = loggerFactory.CreateLogger("Microsoft.Namespace");
                microsoftLogger.LogTrace("Microsoft trace log");
                microsoftLogger.LogInformation("Microsoft information log");
                microsoftLogger.LogWarning("Microsoft warning log");

                var customLogger = loggerFactory.CreateLogger("Custom.Namespace");
                customLogger.LogTrace("Custom trace log");
                customLogger.LogInformation("Custom information log");
                customLogger.LogWarning("Custom warning log");

                var specificLogger = loggerFactory.CreateLogger("Specific.Namespace");
                specificLogger.LogTrace("Specific trace log");
                specificLogger.LogInformation("Specific information log");
                specificLogger.LogWarning("Specific warning log");

                TelemetryConfiguration.Active.TelemetryChannel = null;
            }));
            app.Run(async (context) =>
            {
                logger.LogDebug("Received request: " + context.Request.Method + " " + context.Request.Path);

                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello World - " + DateTimeOffset.Now + Environment.NewLine);
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Address:" + Environment.NewLine);
                await context.Response.WriteAsync("Scheme: " + context.Request.Scheme + Environment.NewLine);
                await context.Response.WriteAsync("Host: " + context.Request.Headers["Host"] + Environment.NewLine);
                await context.Response.WriteAsync("PathBase: " + context.Request.PathBase.Value + Environment.NewLine);
                await context.Response.WriteAsync("Path: " + context.Request.Path.Value + Environment.NewLine);
                await context.Response.WriteAsync("Query: " + context.Request.QueryString.Value + Environment.NewLine);
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Connection:" + Environment.NewLine);
                await context.Response.WriteAsync("RemoteIp: " + context.Connection.RemoteIpAddress + Environment.NewLine);
                await context.Response.WriteAsync("RemotePort: " + context.Connection.RemotePort + Environment.NewLine);
                await context.Response.WriteAsync("LocalIp: " + context.Connection.LocalIpAddress + Environment.NewLine);
                await context.Response.WriteAsync("LocalPort: " + context.Connection.LocalPort + Environment.NewLine);
                await context.Response.WriteAsync("ClientCert: " + context.Connection.ClientCertificate + Environment.NewLine);
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("User: " + context.User.Identity.Name + Environment.NewLine);
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Headers:" + Environment.NewLine);
                foreach (var header in context.Request.Headers)
                {
                    await context.Response.WriteAsync(header.Key + ": " + header.Value + Environment.NewLine);
                }
                await context.Response.WriteAsync(Environment.NewLine);

                await context.Response.WriteAsync("Environment Variables:" + Environment.NewLine);
                var vars = Environment.GetEnvironmentVariables();
                foreach (var key in vars.Keys.Cast<string>().OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
                {
                    var value = vars[key];
                    await context.Response.WriteAsync(key + ": " + value + Environment.NewLine);
                }
                await context.Response.WriteAsync(Environment.NewLine);
            });
        }

        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .Build();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config)
                .Build();

            host.Run();
        }
    }
}

