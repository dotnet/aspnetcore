// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Google;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="GoogleMiddleware"/>.
    /// </summary>
    public static class GoogleAppBuilderExtensions
    {
        /// <summary>
        /// Authenticate users using Google OAuth 2.0.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="options">The Middleware options.</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseGoogleAuthentication(this IApplicationBuilder app, GoogleOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<GoogleMiddleware>(options);
        }

        /// <summary>
        /// Authenticate users using Google OAuth 2.0.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="configureOptions">Used to configure Middleware options.</param>
        /// <param name="optionsName">Name of the options instance to be used</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseGoogleAuthentication(this IApplicationBuilder app, Action<GoogleOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var options = new GoogleOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseGoogleAuthentication(options);
        }
    }
}