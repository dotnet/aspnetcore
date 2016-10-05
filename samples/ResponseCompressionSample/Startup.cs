// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ResponseCompressionSample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ICompressionProvider, GzipCompressionProvider>();
            services.AddTransient<ICompressionProvider, CustomCompressionProvider>();
            services.AddResponseCompression("text/plain", "text/html");
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseResponseCompression();

            app.Map("/testfile1kb.txt", fileApp =>
            {
                fileApp.Run(context =>
                {
                    context.Response.ContentType = "text/plain";
                    return context.Response.SendFileAsync("testfile1kb.txt");
                });
            });

            app.Map("/trickle", trickleApp =>
            {
                trickleApp.Run(async context =>
                {
                    context.Response.ContentType = "text/plain";
                    // Disables compression on net451 because that GZipStream does not implement Flush.
                    context.Features.Get<IHttpBufferingFeature>()?.DisableResponseBuffering();

                    for (int i = 0; i < 100; i++)
                    {
                        await context.Response.WriteAsync("a");
                        await context.Response.Body.FlushAsync();
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                });
            });

            app.Run(async context =>
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(LoremIpsum.Text);
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.UseConnectionLogging();
                })
                // .UseWebListener()
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole(LogLevel.Debug);
                })
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
