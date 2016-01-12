// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Hosting
{
    public static class IISPlatformHandlerAddressExtensions
    {
        // This is defined by IIS's HttpPlatformHandler.
        private static readonly string ServerPort = "HTTP_PLATFORM_PORT";
        private static readonly string ServerPath = "HTTP_PLATFORM_APPL_PATH";

        /// <summary>
        /// Configures the port and base path the server should listen on when running behind HttpPlatformHandler.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IWebApplicationBuilder UseIISPlatformHandlerUrl(this IWebApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var port = Environment.GetEnvironmentVariable(ServerPort);
            var path = Environment.GetEnvironmentVariable(ServerPath);

            if (!string.IsNullOrEmpty(port))
            {
                var address = "http://localhost:" + port + path;
                app.UseSetting(WebApplicationDefaults.ServerUrlsKey, address);
            }

            return app;
        }
    }
}
