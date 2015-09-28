// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.OAuth;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="OAuthMiddleware"/>
    /// </summary>
    public static class OAuthExtensions
    {
        /// <summary>
        /// Authenticate users using OAuth.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="configureOptions">Configures the middleware options.</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseOAuthAuthentication(this IApplicationBuilder app, Action<OAuthOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var options = new OAuthOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseOAuthAuthentication(options);
        }

        /// <summary>
        /// Authenticate users using OAuth.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="options">The middleware configuration options.</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseOAuthAuthentication(this IApplicationBuilder app, OAuthOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<OAuthMiddleware<OAuthOptions>>(options);
        }
    }
}
