// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpOverrides;

namespace HttpOverridesSample;

public class Startup
{
    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });
        app.UseHttpMethodOverride();

        app.Run(async (context) =>
        {
            foreach (var header in context.Request.Headers)
            {
                await context.Response.WriteAsync($"{header.Key}: {header.Value}\r\n");
            }
            await context.Response.WriteAsync($"Method: {context.Request.Method}\r\n");
            await context.Response.WriteAsync($"Scheme: {context.Request.Scheme}\r\n");
            await context.Response.WriteAsync($"RemoteIP: {context.Connection.RemoteIpAddress}\r\n");
            await context.Response.WriteAsync($"RemotePort: {context.Connection.RemotePort}\r\n");
        });
    }

    // Entry point for the application.
    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseKestrel()
                // .UseIIS() // This repo can no longer reference IIS because IISIntegration depends on it.
                .UseStartup<Startup>();
            })
            .Build();

        return host.RunAsync();
    }
}

