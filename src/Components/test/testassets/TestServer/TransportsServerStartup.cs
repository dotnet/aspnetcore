// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TestServer
{
    public class TransportsServerStartup : ServerStartup
    {
        public TransportsServerStartup(IConfiguration configuration)
            : base (configuration)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResourceRequestLog resourceRequestLog)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Map("/defaultTransport", app =>
            {
                app.UseStaticFiles();

                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapBlazorHub();
                    endpoints.MapFallbackToPage("/_ServerHost");
                });
            });

            app.Map("/longPolling", app =>
            {
                app.UseStaticFiles();

                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapBlazorHub(configureOptions: options =>
                    {
                        options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                    });
                    endpoints.MapFallbackToPage("/_ServerHost");
                });
            });
        }
    }
}
