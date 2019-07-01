// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RequestThrottlingSample
{
    public class Startup
    {
        static IConfiguration _config;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddStackQueue((options) =>
            {
                options.MaxConcurrentRequests = Math.Max(1, _config.GetValue<int>("maxConcurrent"));
                options.RequestQueueLimit = Math.Max(1, _config.GetValue<int>("maxQueue"));
            });

            services.AddLogging();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseRequestThrottling();
            app.Run(async context =>
            {
                await context.Response.WriteAsync("Hello Request Throttling! If you rapidly refresh this page, it will 503.");
                await Task.Delay(400);
            });
        }

        // Entry point for the application.
        public static void Main(string[] args)
        {
            _config = new ConfigurationBuilder()
                            .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                            .AddCommandLine(args)
                            .Build();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory()) // for the cert file
                .ConfigureLogging(factory =>
                {
                    factory.SetMinimumLevel(LogLevel.Debug);
                    factory.AddConsole();
                })
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
