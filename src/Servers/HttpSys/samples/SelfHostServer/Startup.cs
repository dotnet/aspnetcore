// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SelfHostServer;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Server options can be configured here instead of in Main.
        services.Configure<HttpSysOptions>(options =>
        {
            options.Authentication.Schemes = AuthenticationSchemes.None;
            options.Authentication.AllowAnonymous = true;
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Hello world from " + context.Request.Host + " at " + DateTime.Now);
        });
    }
}
