// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketsSample.EndPoints;
using SocketsSample.Hubs;
using SocketsSample.Protobuf;

namespace SocketsSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ProtobufInvocationAdapter>();
            services.AddSingleton<LineInvocationAdapter>();

            services.AddSockets();

            services.AddSignalR(options =>
                    {
                        options.RegisterInvocationAdapter<ProtobufInvocationAdapter>("protobuf");
                        options.RegisterInvocationAdapter<LineInvocationAdapter>("line");
                    });
            // .AddRedis();

            services.AddSingleton<ChatEndPoint>();
            services.AddSingleton<MessagesEndPoint>();
            services.AddSingleton<ProtobufSerializer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);

            app.UseFileServer();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSignalR(routes =>
            {
                routes.MapHub<Chat>("/hubs");
            });

            app.UseSockets(routes =>
            {
                routes.MapEndpoint<ChatEndPoint>("/chat");
                routes.MapEndpoint<MessagesEndPoint>("/msgs");
            });
        }
    }
}
