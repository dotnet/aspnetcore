// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TestServer;

/// <summary>
/// Implementation of <see cref="ServerStartup"/> intended for tests that check behavior with Content-Security-Policy headers.
/// </summary>
public class ServerStartupWithCsp : ServerStartup
{
    public ServerStartupWithCsp(IConfiguration configuration) : base(configuration)
    {
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env, ResourceRequestLog resourceRequestLog)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.ContentSecurityPolicy = "style-src 'self';";
            await next();
        });

        base.Configure(app, env, resourceRequestLog);
    }
}
