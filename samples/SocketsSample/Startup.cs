// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocketsSample.EndPoints;
using SocketsSample.Hubs;

namespace SocketsSample
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSockets();

            services.AddSignalR();
            // .AddRedis();

            services.AddCors(o =>
            {
                o.AddPolicy("Everything", p =>
                {
                    p.AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowAnyOrigin();
                });
            });

            services.AddEndPoint<MessagesEndPoint>();
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
                routes.MapHub<Chat>("hubs");
                routes.MapHub<Streaming>("streaming");
            });

            app.UseSockets(routes =>
            {
                routes.MapEndPoint<MessagesEndPoint>("chat");
            });
        }
    }
}
