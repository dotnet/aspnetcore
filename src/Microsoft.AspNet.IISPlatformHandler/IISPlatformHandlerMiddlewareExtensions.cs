// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.IISPlatformHandler;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNet.Builder
{
    public static class IISPlatformHandlerMiddlewareExtensions
    {
        /// <summary>
        /// Adds middleware for interacting with the IIS HttpPlatformHandler reverse proxy module.
        /// This will handle forwarded Windows Authentication, request scheme, remote IPs, etc..
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseIISPlatformHandler(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<IISPlatformHandlerMiddleware>();
        }

        /// <summary>
        /// Adds middleware for interacting with the IIS HttpPlatformHandler reverse proxy module.
        /// This will handle forwarded Windows Authentication, request scheme, remote IPs, etc..
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseIISPlatformHandler(this IApplicationBuilder app, IISPlatformHandlerOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<IISPlatformHandlerMiddleware>(Options.Create(options));
        }
    }
}
