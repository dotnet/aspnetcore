// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IISSample;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // These two middleware are registered via an IStartupFilter in UseIISIntegration but you can configure them here.
        services.Configure<IISOptions>(options =>
        {
            options.AuthenticationDisplayName = "Windows Auth";
        });
        services.Configure<ForwardedHeadersOptions>(options =>
        {
        });
    }

    public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory, IAuthenticationSchemeProvider authSchemeProvider)
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
            await context.Response.WriteAsync("Host: " + context.Request.Headers["Host"] + Environment.NewLine);
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
            var scheme = await authSchemeProvider.GetSchemeAsync(IISDefaults.AuthenticationScheme);
            await context.Response.WriteAsync("DisplayName: " + scheme?.DisplayName + Environment.NewLine);
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
            if (context.Features.Get<IHttpUpgradeFeature>() != null)
            {
                await context.Response.WriteAsync("Websocket feature is enabled.");
            }
            else
            {
                await context.Response.WriteAsync("Websocket feature is disabled.");
            }
        });
    }

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuidler =>
            {
                webHostBuidler
                    .UseKestrel()
                    .UseStartup<Startup>();
            })
            .ConfigureLogging((_, factory) =>
            {
                factory.AddConsole();
                factory.AddFilter("Console", level => level >= LogLevel.Debug);
            })
            .Build();

        return host.RunAsync();
    }
}

