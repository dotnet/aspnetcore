// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.MicrosoftAccount;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods to add Microsoft Account authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class MicrosoftAccountAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="MicrosoftAccountMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Microsoft Account authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">A <see cref="MicrosoftAccountOptions"/> that specifies options for the middleware.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseMicrosoftAccountAuthentication(this IApplicationBuilder app, MicrosoftAccountOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<MicrosoftAccountMiddleware>(options);
        }

        /// <summary>
        /// Adds the <see cref="MicrosoftAccountMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Microsoft Account authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="configureOptions">An action delegate to configure the provided <see cref="MicrosoftAccountOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseMicrosoftAccountAuthentication(this IApplicationBuilder app, Action<MicrosoftAccountOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var options = new MicrosoftAccountOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseMicrosoftAccountAuthentication(options);
        }
    }
}
