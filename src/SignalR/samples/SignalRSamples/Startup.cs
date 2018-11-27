// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SignalRSamples.ConnectionHandlers;
using SignalRSamples.Hubs;

namespace SignalRSamples
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConnections();

            services.AddSignalR(options =>
            {
                // Faster pings for testing
                options.KeepAliveInterval = TimeSpan.FromSeconds(5);
            })
            .AddMessagePackProtocol();
            //.AddRedis();

            services.AddCors(o =>
            {
                o.AddPolicy("Everything", p =>
                {
                    p.AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowAnyOrigin()
                     .AllowCredentials();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseFileServer();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("Everything");

            app.UseSignalR(routes =>
            {
                routes.MapHub<DynamicChat>("/dynamic");
                routes.MapHub<Chat>("/default");
                routes.MapHub<Streaming>("/streaming");
                routes.MapHub<HubTChat>("/hubT");
            });

            app.UseConnections(routes =>
            {
                routes.MapConnectionHandler<MessagesConnectionHandler>("/chat");
            });
        }
    }
}
