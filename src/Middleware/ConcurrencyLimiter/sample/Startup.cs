// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConcurrencyLimiterSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLIFOQueue((options) => {
                options.MaxConcurrentRequests = Environment.ProcessorCount;
                options.RequestQueueLimit = 50;
            });
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseConcurrencyLimiter();
            app.Run(async context =>
            {
                var delay = 100;
                Task.Delay(delay).Wait();

                await context.Response.WriteAsync("Hello World!");
            });
        }

        public static void Main(string[] args)
        {
            new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory()) // for cert file
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
