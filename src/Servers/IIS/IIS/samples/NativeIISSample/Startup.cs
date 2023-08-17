// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NativeIISSample;

public class Startup
{
    private readonly IAuthenticationSchemeProvider _authSchemeProvider;

    public Startup(IAuthenticationSchemeProvider authSchemeProvider = null)
    {
        _authSchemeProvider = authSchemeProvider;
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app)
    {
        app.Run(async (context) =>
        {
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
            if (_authSchemeProvider != null)
            {
                var scheme = await _authSchemeProvider.GetSchemeAsync(IISServerDefaults.AuthenticationScheme);
                await context.Response.WriteAsync("DisplayName: " + scheme?.DisplayName + Environment.NewLine);
            }

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

            // accessing IIS server variables
            await context.Response.WriteAsync("Server Variables:" + Environment.NewLine);

            foreach (var varName in IISServerVarNames)
            {
                await context.Response.WriteAsync(varName + ": " + context.GetServerVariable(varName) + Environment.NewLine);
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

            await context.Response.WriteAsync(Environment.NewLine);
            var server = context.RequestServices.GetService<IServer>();

            var addresses = server.Features.Get<IServerAddressesFeature>();
            foreach (var key in addresses.Addresses)
            {
                await context.Response.WriteAsync(key + Environment.NewLine);
            }

            if (server.Features.Get<IIISEnvironmentFeature>() is { } envFeature)
            {
                await context.Response.WriteAsync(Environment.NewLine);
                await context.Response.WriteAsync("IIS Environment Information:" + Environment.NewLine);
                await context.Response.WriteAsync("IIS Version: " + envFeature.IISVersion + Environment.NewLine);
                await context.Response.WriteAsync("ApplicationId: " + envFeature.ApplicationId + Environment.NewLine);
                await context.Response.WriteAsync("Application Path: " + envFeature.ApplicationPhysicalPath + Environment.NewLine);
                await context.Response.WriteAsync("Application Virtual Path: " + envFeature.ApplicationVirtualPath + Environment.NewLine);
                await context.Response.WriteAsync("Application Config Path: " + envFeature.AppConfigPath + Environment.NewLine);
                await context.Response.WriteAsync("AppPool ID: " + envFeature.AppPoolId + Environment.NewLine);
                await context.Response.WriteAsync("AppPool Config File: " + envFeature.AppPoolConfigFile + Environment.NewLine);
                await context.Response.WriteAsync("Site ID: " + envFeature.SiteId + Environment.NewLine);
                await context.Response.WriteAsync("Site Name: " + envFeature.SiteName + Environment.NewLine);
            }
            else
            {
                await context.Response.WriteAsync($"No {nameof(IIISEnvironmentFeature)} available." + Environment.NewLine);
            }
        });
    }

    private static readonly string[] IISServerVarNames =
    {
            "AUTH_TYPE",
            "AUTH_USER",
            "CONTENT_TYPE",
            "HTTP_HOST",
            "HTTPS",
            "REMOTE_PORT",
            "REMOTE_USER",
            "REQUEST_METHOD",
            "WEBSOCKET_VERSION"
        };

    public static Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel()
                    .UseIIS()
                    .UseIISIntegration()
                    .UseStartup<Startup>();
            })
            .Build();

        return host.RunAsync();
    }
}
