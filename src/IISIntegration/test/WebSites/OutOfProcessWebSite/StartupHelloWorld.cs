// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestSites
{
    public class StartupHelloWorld
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.Run(async ctx =>
            {
                if (ctx.Request.Path.Value.StartsWith("/Path"))
                {
                    await ctx.Response.WriteAsync(ctx.Request.Path.Value);
                    return;
                }
                if (ctx.Request.Path.Value.StartsWith("/Query"))
                {
                    await ctx.Response.WriteAsync(ctx.Request.QueryString.Value);
                    return;
                }
                if (ctx.Request.Path.Value.StartsWith("/BodyLimit"))
                {
                    await ctx.Response.WriteAsync(
                        ctx.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize?.ToString() ?? "null");
                    return;
                }

                if (ctx.Request.Path.StartsWithSegments("/Auth"))
                {
                    var iisAuth = Environment.GetEnvironmentVariable("ASPNETCORE_IIS_HTTPAUTH");
                    var authProvider = ctx.RequestServices.GetService<IAuthenticationSchemeProvider>();
                    var authScheme = (await authProvider.GetAllSchemesAsync()).SingleOrDefault();
                    if (string.IsNullOrEmpty(iisAuth))
                    {
                        await ctx.Response.WriteAsync("backcompat;" + (authScheme?.Name ?? "null"));
                    }
                    else
                    {
                        await ctx.Response.WriteAsync("latest;" + (authScheme?.Name ?? "null"));
                    }
                    return;
                }

                await ctx.Response.WriteAsync("Hello World");
            });
        }
    }
}
