// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TestSites
{
    public class StartupHelloWorld
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            app.Run(ctx =>
            {
                if (ctx.Request.Path.Value.StartsWith("/Path"))
                {
                    return ctx.Response.WriteAsync(ctx.Request.Path.Value);
                }
                if (ctx.Request.Path.Value.StartsWith("/Query"))
                {
                    return ctx.Response.WriteAsync(ctx.Request.QueryString.Value);
                }

                return ctx.Response.WriteAsync("Hello World");
            });
        }
    }
}