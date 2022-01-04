// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace IISSample;

public class Startup
{
    public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
    {
        var logger = loggerfactory.CreateLogger("Requests");

        app.Run(async (context) =>
        {
            logger.LogDebug("Received request: " + context.Request.Method + " " + context.Request.Path);

            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Hello World - " + DateTimeOffset.Now + Environment.NewLine);
            await context.Response.WriteAsync(Environment.NewLine);

            await context.Response.WriteAsync("Address:" + Environment.NewLine);
            await context.Response.WriteAsync("Scheme: " + context.Request.Scheme + Environment.NewLine);
            await context.Response.WriteAsync("Host: " + context.Request.Headers.Host + Environment.NewLine);
            await context.Response.WriteAsync("PathBase: " + context.Request.PathBase.Value + Environment.NewLine);
            await context.Response.WriteAsync("Path: " + context.Request.Path.Value + Environment.NewLine);
            await context.Response.WriteAsync("Query: " + context.Request.QueryString.Value + Environment.NewLine);
            await context.Response.WriteAsync(Environment.NewLine);

            await context.Response.WriteAsync("Connection:" + Environment.NewLine);
            await context.Response.WriteAsync("RemoteIp: " + context.Connection.RemoteIpAddress + Environment.NewLine);
            await context.Response.WriteAsync("RemotePort: " + context.Connection.RemotePort + Environment.NewLine);
            await context.Response.WriteAsync("LocalIp: " + context.Connection.LocalIpAddress + Environment.NewLine);
            await context.Response.WriteAsync("LocalPort: " + context.Connection.LocalPort + Environment.NewLine);
            await context.Response.WriteAsync("ClientCert: " + context.Connection.ClientCertificate + Environment.NewLine);
            await context.Response.WriteAsync(Environment.NewLine);

            await context.Response.WriteAsync("User: " + context.User.Identity.Name + Environment.NewLine);
            await context.Response.WriteAsync(Environment.NewLine);

            await context.Response.WriteAsync("Headers:" + Environment.NewLine);
            foreach (var header in context.Request.Headers)
            {
                await context.Response.WriteAsync(header.Key + ": " + header.Value + Environment.NewLine);
            }
            await context.Response.WriteAsync(Environment.NewLine);

            await context.Response.WriteAsync("Environment Variables:" + Environment.NewLine);
            var vars = Environment.GetEnvironmentVariables();
            foreach (var key in vars.Keys.Cast<string>().OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
            {
                var value = vars[key];
                await context.Response.WriteAsync(key + ": " + value + Environment.NewLine);
            }
            await context.Response.WriteAsync(Environment.NewLine);
        });
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .UseStartup<Startup>();
            })
            .ConfigureLogging(factory =>
            {
                factory.AddConsole();
            })
            .Build();

        return host.RunAsync();
    }
}

