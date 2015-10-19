// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Twitter;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods to add Twitter authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class TwitterAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="TwitterMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Twitter authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="configureOptions">An action delegate to configure the provided <see cref="TwitterOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseTwitterAuthentication(this IApplicationBuilder app, Action<TwitterOptions> configureOptions = null)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var options = new TwitterOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseTwitterAuthentication(options);
        }

        /// <summary>
        /// Adds the <see cref="TwitterMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Twitter authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">A <see cref="TwitterOptions"/> that specifies options for the middleware.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseTwitterAuthentication(this IApplicationBuilder app, TwitterOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<TwitterMiddleware>(options);
        }
    }
}
