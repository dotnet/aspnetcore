// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Cookies;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods to add cookie authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class CookieAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="CookieAuthenticationMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables cookie authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseCookieAuthentication(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseCookieAuthentication(new CookieAuthenticationOptions());
        }

        /// <summary>
        /// Adds the <see cref="CookieAuthenticationMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables cookie authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="configureOptions">An action delegate to configure the provided <see cref="CookieAuthenticationOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseCookieAuthentication(this IApplicationBuilder app, Action<CookieAuthenticationOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var options = new CookieAuthenticationOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseCookieAuthentication(options);
        }

        /// <summary>
        /// Adds the <see cref="CookieAuthenticationMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables cookie authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">A <see cref="JwtBearerOptions"/> that specifies options for the middleware.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseCookieAuthentication(this IApplicationBuilder app, CookieAuthenticationOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<CookieAuthenticationMiddleware>(options);
        }
    }
}