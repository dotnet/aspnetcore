// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Facebook;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods to add Facebook authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class FacebookAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="FacebookMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Facebook authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">A <see cref="FacebookOptions"/> that specifies options for the middleware.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
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
        /// Adds the <see cref="FacebookMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Facebook authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="configureOptions">An action delegate to configure the provided <see cref="FacebookOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
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
