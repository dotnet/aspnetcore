// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StaticFilesSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDirectoryBrowser();
            services.AddResponseCompression();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment host)
        {
            app.UseResponseCompression();

            app.UseFileServer(new FileServerOptions
            {
                EnableDirectoryBrowsing = true
            });
        }

        public static Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .ConfigureLogging(factory =>
                    {
                        factory.AddFilter("Console", level => level >= LogLevel.Debug);
                        factory.AddConsole();
                    })
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseKestrel()
                    // .UseHttpSys()
                    .UseIISIntegration()
                    .UseStartup<Startup>();
                }).Build();

            return host.RunAsync();
        }
    }
}
