// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Facebook;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="FacebookMiddleware"/>.
    /// </summary>
    public static class FacebookAppBuilderExtensions
    {
        /// <summary>
        /// Authenticate users using Facebook.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseFacebookAuthentication(this IApplicationBuilder app, FacebookOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<FacebookMiddleware>(options);
        }

        /// <summary>
        /// Authenticate users using Facebook.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="configureOptions">Configures the options.</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseFacebookAuthentication(this IApplicationBuilder app, Action<FacebookOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var options = new FacebookOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseFacebookAuthentication(options);
        }
    }
}
