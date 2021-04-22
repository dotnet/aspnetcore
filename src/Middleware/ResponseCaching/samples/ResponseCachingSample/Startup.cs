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
using Microsoft.Net.Http.Headers;

namespace ResponseCachingSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCaching();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseResponseCaching();
            app.Run(async (context) =>
            {
                context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = TimeSpan.FromSeconds(10)
                };
                context.Response.Headers[HeaderNames.Vary] = new string[] { "Accept-Encoding" };

                await context.Response.WriteAsync("Hello World! " + DateTime.UtcNow);
            });
        }

        public static Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .UseStartup<Startup>();
                }).Build();

            return host.RunAsync();
        }
    }
}
