// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HostedInAspNet.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<BootResourceRequestLog>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, BootResourceRequestLog bootResourceRequestLog)
        {
            app.Use((context, next) =>
            {
                // This is used by E2E tests to verify that the correct resources were fetched,
                // and that it was possible to override the loading mechanism
                if (context.Request.Query.ContainsKey("customizedbootresource")
                    || context.Request.Headers.ContainsKey("customizedbootresource")
                    || context.Request.Path.Value.EndsWith("/blazor.boot.json", StringComparison.Ordinal))
                {
                    bootResourceRequestLog.AddRequest(context.Request);
                }
                return next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
