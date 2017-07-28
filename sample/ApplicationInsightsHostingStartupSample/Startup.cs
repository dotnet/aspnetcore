// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        public void ConfigureJavaScript(IApplicationBuilder app)
        {
            app.UseMvcWithDefaultRoute();
        }

        public void ConfigureDefaultLogging(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            ConfigureLoggingMiddleware(app, loggerFactory);
        }

        public void ConfigureCustomLogging(IApplicationBuilder app, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            loggerFactory.AddApplicationInsights(serviceProvider, (s, level) => s.Contains("o"));
            ConfigureLoggingMiddleware(app, loggerFactory);
        }

        private static void ConfigureLoggingMiddleware(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.Map("/log", logApp => logApp.Run(context =>
            {
                var oldChannel = TelemetryConfiguration.Active.TelemetryChannel;
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

                TelemetryConfiguration.Active.TelemetryChannel = oldChannel;

                return Task.CompletedTask;
            }));
        }

        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddCommandLine(args)
                .Build();

            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .ConfigureLogging((hostingContext, builder) =>
                {
                    builder.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    builder.AddConsole();
                })
                .UseKestrel()
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .Build();

            host.Run();
        }
    }
}

