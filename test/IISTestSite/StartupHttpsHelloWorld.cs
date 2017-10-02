// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IISTestSite
{
    public class StartupHttpsHelloWorld
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.Run(async ctx =>
            {
                if (ctx.Request.Path.Value.StartsWith("/ClientCertificate"))
                {
                    await ctx.Response.WriteAsync($"{ ctx.Request.Scheme }Hello World, Cert: {ctx.Connection.ClientCertificate != null}");
                    return;
                }
                await ctx.Response.WriteAsync(ctx.Request.Scheme + "Hello World");
            });
        }
    }
}
