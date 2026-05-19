// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Extensions;

namespace SampleApp;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {

    }

    public void Configure(IApplicationBuilder app, IConfiguration config)
    {
        app.Run(async (context) =>
        {
            await context.Response.WriteAsync($"Hello from {context.Request.GetDisplayUrl()}\r\n");
            await context.Response.WriteAsync("\r\n");

            await context.Response.WriteAsync("Headers:\r\n");
            foreach (var header in context.Request.Headers)
            {
                await context.Response.WriteAsync($"{header.Key}: {header.Value}\r\n");
            }
            await context.Response.WriteAsync("\r\n");

            await context.Response.WriteAsync("Connection:\r\n");
            await context.Response.WriteAsync("RemoteIp: " + context.Connection.RemoteIpAddress + "\r\n");
            await context.Response.WriteAsync("RemotePort: " + context.Connection.RemotePort + "\r\n");
            await context.Response.WriteAsync("LocalIp: " + context.Connection.LocalIpAddress + "\r\n");
            await context.Response.WriteAsync("LocalPort: " + context.Connection.LocalPort + "\r\n");
            await context.Response.WriteAsync("ClientCert: " + context.Connection.ClientCertificate + "\r\n");
            await context.Response.WriteAsync("\r\n");

            await context.Response.WriteAsync("Environment Variables:\r\n");
            var vars = Environment.GetEnvironmentVariables();
            foreach (var key in vars.Keys.Cast<string>().OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
            {
                var value = vars[key];
                await context.Response.WriteAsync($"{key}: {value}\r\n");
            }
            await context.Response.WriteAsync("\r\n");

            await context.Response.WriteAsync("Config:\r\n");
            await ShowConfig(context.Response, config);
            await context.Response.WriteAsync("\r\n");
        });
    }

    private static async Task ShowConfig(HttpResponse response, IConfiguration config)
    {
        foreach (var pair in config.GetChildren())
        {
            await response.WriteAsync($"{pair.Path}: {pair.Value}\r\n");
            await ShowConfig(response, pair);
        }
    }
}
