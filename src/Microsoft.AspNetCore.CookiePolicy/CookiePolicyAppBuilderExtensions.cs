// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to add cookie policy capabilities to an HTTP application pipeline.
    /// </summary>
    public static class CookiePolicyAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="CookiePolicyMiddleware"/> handler to the specified <see cref="IApplicationBuilder"/>, which enables cookie policy capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseCookiePolicy(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<CookiePolicyMiddleware>();
        }

        /// <summary>
        /// Adds the <see cref="CookiePolicyMiddleware"/> handler to the specified <see cref="IApplicationBuilder"/>, which enables cookie policy capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
        /// <param name="options">A <see cref="CookiePolicyOptions"/> that specifies options for the handler.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseCookiePolicy(this IApplicationBuilder app, CookiePolicyOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<CookiePolicyMiddleware>(Options.Create(options));
        }
    }
}