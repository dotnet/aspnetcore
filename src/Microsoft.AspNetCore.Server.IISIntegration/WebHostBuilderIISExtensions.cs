// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostBuilderIISExtensions
    {
        // These are defined as ASPNETCORE_ environment variables by IIS's AspNetCoreModule.
        private static readonly string ServerPort = "PORT";
        private static readonly string ServerPath = "APPL_PATH";
        private static readonly string PairingToken = "TOKEN";

        /// <summary>
        /// Configures the port and base path the server should listen on when running behind AspNetCoreModule.
        /// The app will also be configured to capture startup errors.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseIISIntegration(this IWebHostBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var port = app.GetSetting(ServerPort) ?? Environment.GetEnvironmentVariable($"ASPNETCORE_{ServerPort}");
            var path = app.GetSetting(ServerPath) ?? Environment.GetEnvironmentVariable($"ASPNETCORE_{ServerPath}");
            var pairingToken = app.GetSetting(PairingToken) ?? Environment.GetEnvironmentVariable($"ASPNETCORE_{PairingToken}");

            if (!string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(pairingToken))
            {
                var address = "http://localhost:" + port + path;
                app.UseSetting(WebHostDefaults.ServerUrlsKey, address);
                app.CaptureStartupErrors(true);

                app.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter>(new IISSetupFilter(pairingToken));
                    services.Configure<ForwardedHeadersOptions>(options =>
                    {
                        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                        // https://github.com/aspnet/IISIntegration/issues/140
                        // Azure Web Sites needs to be treated specially as we get an imbalanced set of X-Forwarded-* headers.
                        // We use the existence of the %WEBSITE_INSTANCE_ID% environment variable to determine if we're running
                        // in this environment, and if so we disable the symmetry check.
                        var isAzure = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID"));
                        options.RequireHeaderSymmetry = !isAzure;
                    });
                });
            }

            return app;
        }
    }
}
