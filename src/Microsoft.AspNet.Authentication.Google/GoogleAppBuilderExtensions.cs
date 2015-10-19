// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Google;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods to add Google authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class GoogleAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="GoogleMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Google authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">A <see cref="GoogleOptions"/> that specifies options for the middleware.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
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
        /// Adds the <see cref="GoogleMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Google authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="configureOptions">An action delegate to configure the provided <see cref="GoogleOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
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
