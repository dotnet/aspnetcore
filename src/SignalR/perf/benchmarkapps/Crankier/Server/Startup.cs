// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Crankier.Server
{
    public class Startup
    {
        private readonly IConfiguration _config;
        private readonly string _azureSignalrConnectionString;
        public Startup(IConfiguration configuration)
        {
            _config = configuration;
            _azureSignalrConnectionString = configuration.GetSection("Azure:SignalR").GetValue<string>("ConnectionString", null);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var signalrBuilder = services.AddSignalR();

            if (_azureSignalrConnectionString != null)
            {
                signalrBuilder.AddAzureSignalR();
            }

            signalrBuilder.AddMessagePackProtocol();

            services.AddSingleton<ConnectionCounter>();

            services.AddHostedService<ConnectionCounterHostedService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            if (_azureSignalrConnectionString != null)
            {
                app.UseAzureSignalR(routes => {
                    routes.MapHub<EchoHub>("/echo");
                });
            }
            else
            {
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHub<EchoHub>("/echo");
                });
            }
        }
    }
}
