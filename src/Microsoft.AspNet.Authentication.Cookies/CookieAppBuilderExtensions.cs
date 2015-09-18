// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods provided by the cookies authentication middleware
    /// </summary>
    public static class CookieAppBuilderExtensions
    {
        /// <summary>
        /// Adds a cookie-based authentication middleware to your web application pipeline.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to your configuration method</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseCookieAuthentication([NotNull] this IApplicationBuilder app)
        {
            return app.UseCookieAuthentication(new CookieAuthenticationOptions());
        }

        /// <summary>
        /// Adds a cookie-based authentication middleware to your web application pipeline.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to your configuration method</param>
        /// <param name="configureOptions">Used to configure the options for the middleware</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseCookieAuthentication([NotNull] this IApplicationBuilder app, Action<CookieAuthenticationOptions> configureOptions)
        {
            var options = new CookieAuthenticationOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseCookieAuthentication(options);
        }

        /// <summary>
        /// Adds a cookie-based authentication middleware to your web application pipeline.
        /// </summary>
        /// <param name="app">The IApplicationBuilder passed to your configuration method</param>
        /// <param name="options">Used to configure the middleware</param>
        /// <returns>The original app parameter</returns>
        public static IApplicationBuilder UseCookieAuthentication([NotNull] this IApplicationBuilder app, [NotNull] CookieAuthenticationOptions options)
        {
            return app.UseMiddleware<CookieAuthenticationMiddleware>(options);
        }
    }
}