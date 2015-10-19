// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.CookiePolicy;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods to add cookie policy capabilities to an HTTP application pipeline.
    /// </summary>
    public static class CookiePolicyAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="CookiePolicyMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables cookie policy capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">A <see cref="CookiePolicyOptions"/> that specifies options for the middleware.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseCookiePolicy(this IApplicationBuilder app, CookiePolicyOptions options)
        {
            return app.UseMiddleware<CookiePolicyMiddleware>(options);
        }

        /// <summary>
        /// Adds the <see cref="CookiePolicyMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables cookie policy capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="configureOptions">An action delegate to configure the provided <see cref="CookiePolicyOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseCookiePolicy(this IApplicationBuilder app, Action<CookiePolicyOptions> configureOptions)
        {
            var options = new CookiePolicyOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseCookiePolicy(options);
        }
    }
}