// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    public static class IISMiddlewareExtensions
    {
        /// <summary>
        /// Adds middleware for interacting with the IIS AspNetCoreModule reverse proxy module.
        /// This will handle forwarded Windows Authentication, client certificates, etc..
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseIIS(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<IISMiddleware>();
        }

        /// <summary>
        /// Adds middleware for interacting with the IIS AspNetCoreModule reverse proxy module.
        /// This will handle forwarded Windows Authentication, client certificates, etc..
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseIIS(this IApplicationBuilder app, IISOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<IISMiddleware>(Options.Create(options));
        }
    }
}
