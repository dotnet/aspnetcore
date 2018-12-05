// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkServer.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BenchmarkServer
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
            .AddMessagePackProtocol();

            var redisConnectionString = _config["SignalRRedis"];
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                signalrBuilder.AddStackExchangeRedis(redisConnectionString);
            }
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseEndpointRouting(builder =>
            {
                MapHub<EchoHub>(builder, "/echo");
            });

            app.UseEndpoint();

            app.UseSignalR(routes =>
            {
                routes.MapHub<EchoHub>("/echo", o =>
                {
                    // Remove backpressure for benchmarking
                    o.TransportMaxBufferSize = 0;
                    o.ApplicationMaxBufferSize = 0;
                });
            });
        }

        public static void MapHub<THub>(IEndpointRouteBuilder routeBuilder, string path) where THub : Hub
        {
            var connectionBuilder = new ConnectionBuilder(routeBuilder.ServiceProvider);
            connectionBuilder.UseHub<THub>();
            var httpConnectionDispatcher = routeBuilder.ServiceProvider.GetRequiredService<HttpConnectionDispatcher>();
            var socket = connectionBuilder.Build();
            var options = new HttpConnectionDispatcherOptions() { TransportMaxBufferSize = 0, ApplicationMaxBufferSize = 0 };
            routeBuilder.MapVerbs(path,
                c => httpConnectionDispatcher.ExecuteAsync(c, options, socket),
                new string[] { "DELETE", "GET", "POST" });
            routeBuilder.MapPost(path + "/negotiate",
                c => httpConnectionDispatcher.ExecuteNegotiateAsync(c, options));
        }
    }
}
