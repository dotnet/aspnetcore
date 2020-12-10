// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HostedInAspNet.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<BootResourceRequestLog>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, BootResourceRequestLog bootResourceRequestLog)
        {
            var mapAlternativePathApp = Configuration.GetValue<bool>("UseAlternativeBasePath");
            var mapAllApps = Configuration.GetValue<bool>("MapAllApps");
            app.Use((context, next) =>
            {
                // This is used by E2E tests to verify that the correct resources were fetched,
                // and that it was possible to override the loading mechanism
                if (context.Request.Query.ContainsKey("customizedbootresource")
                    || context.Request.Headers.ContainsKey("customizedbootresource")
                    || context.Request.Path.Value.EndsWith("/blazor.boot.json"))
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

            if (mapAllApps || mapAlternativePathApp)
            {
                app.UseBlazorFrameworkFiles("/app");
            }

            if(!mapAlternativePathApp || mapAllApps) 
            {
                app.UseBlazorFrameworkFiles();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                if (mapAllApps || mapAlternativePathApp)
                {
                    endpoints.MapFallbackToFile("/app/{**slug:nonfile}", "app/index.html");
                }

                if(!mapAlternativePathApp || mapAllApps)
                {
                    endpoints.MapFallbackToFile("index.html");
                }
            });
        }
    }
}
