// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.Extensions.DependencyInjection;

namespace NativeIISSample
{
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
                var addresses = context.RequestServices.GetService<IServer>().Features.Get<IServerAddressesFeature>();
                foreach (var key in addresses.Addresses)
                {
                    await context.Response.WriteAsync(key + Environment.NewLine);
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

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseIIS()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
