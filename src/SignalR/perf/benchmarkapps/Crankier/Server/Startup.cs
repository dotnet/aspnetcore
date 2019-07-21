// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Crankier
{
    public class Startup
    {
        private readonly IConfiguration _config;
        public Startup(IConfiguration configuration)
        {
            _config = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var signalrBuilder = services.AddSignalR(o =>
            {
                o.EnableDetailedErrors = true;
            })
            // TODO: Json vs NewtonsoftJson option
            .AddMessagePackProtocol();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<EchoHub>("/echo", o =>
                {
                    // Remove backpressure for benchmarking
                    o.TransportMaxBufferSize = 0;
                    o.ApplicationMaxBufferSize = 0;
                });
            });
        }
    }
}
