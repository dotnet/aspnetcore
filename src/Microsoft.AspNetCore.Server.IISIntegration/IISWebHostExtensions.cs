// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    public static class IISWebHostExtensions
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
        public static IWebHostBuilder UseIIS(this IWebHostBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var port = app.GetSetting(ServerPort);
            var path = app.GetSetting(ServerPath);
            var pairingToken = app.GetSetting(PairingToken);

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
                    });
                });
            }

            return app;
        }
    }
}
