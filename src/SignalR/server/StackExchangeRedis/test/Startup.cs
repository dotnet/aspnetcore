// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            })
                .AddMessagePackProtocol()
                .AddStackExchangeRedis(options =>
                {
                    options.Configuration.EndPoints.Add(Environment.GetEnvironmentVariable("REDIS_CONNECTION"));
                });

            services.AddSingleton<IUserIdProvider, UserNameIdProvider>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<EchoHub>("/echo");
            });
        }

        private class UserNameIdProvider : IUserIdProvider
        {
            public string GetUserId(HubConnectionContext connection)
            {
                // This is an AWFUL way to authenticate users! We're just using it for test purposes.
                var userNameHeader = connection.GetHttpContext().Request.Headers["UserName"];
                if (!StringValues.IsNullOrEmpty(userNameHeader))
                {
                    return userNameHeader;
                }

                return null;
            }
        }
    }
}
