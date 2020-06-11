// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NodeServicesExamples
{
    public class Startup
    {
#pragma warning disable 0618
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            // Enable Node Services
            services.AddNodeServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, INodeServices nodeServices)
        {
            app.UseDeveloperExceptionPage();

            // Dynamically transpile any .js files under the '/js/' directory
            app.Use(next => async context =>
            {
                var requestPath = context.Request.Path.Value;
                if (requestPath.StartsWith("/js/") && requestPath.EndsWith(".js"))
                {
                    var fileInfo = env.WebRootFileProvider.GetFileInfo(requestPath);
                    if (fileInfo.Exists)
                    {
                        var transpiled = await nodeServices.InvokeAsync<string>("./Node/transpilation.js", fileInfo.PhysicalPath, requestPath);
                        await context.Response.WriteAsync(transpiled);
                        return;
                    }
                }

                // Not a JS file, or doesn't exist - let some other middleware handle it
                await next.Invoke(context);
            });

            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
#pragma warning restore 0618

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                    factory.AddDebug();
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
