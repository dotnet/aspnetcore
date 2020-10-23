// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
    // Used for E2E tests that verify different overloads of MapFallbackToClientSideBlazor.
    public class HostedInAspNetStartup
    {

        public HostedInAspNetStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ResourceRequestLog>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResourceRequestLog bootResourceRequestLog)
        {
            // The client-side files middleware needs to be here because the base href in hardcoded to /subdir/
            app.Map("/subdir", subApp =>
            {
                subApp.Use((context, next) =>
                {

                    if (context.Request.Path.Value.EndsWith("/Download.txt", StringComparison.Ordinal))
                    {
                        bootResourceRequestLog.AddRequest(context.Request);
                    }

                    return next();
                });

                subApp.UseBlazorFrameworkFiles();
                subApp.UseStaticFiles();

                if (env.IsDevelopment())
                {
                    subApp.UseDeveloperExceptionPage();
                    subApp.UseWebAssemblyDebugging();
                }

                subApp.UseRouting();

                subApp.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToFile("index.html");
                });
            }

            );
        }
    }
}
