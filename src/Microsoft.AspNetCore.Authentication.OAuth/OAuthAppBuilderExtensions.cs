// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to add OAuth 2.0 authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class OAuthAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="OAuthMiddleware{TOptions}"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables OAuth 2.0 authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseOAuthAuthentication(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<OAuthMiddleware<OAuthOptions>>();
        }

        /// <summary>
        /// Adds the <see cref="OAuthMiddleware{TOptions}"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables OAuth 2.0 authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">A <see cref="OAuthOptions"/> that specifies options for the middleware.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
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

            return app.UseMiddleware<OAuthMiddleware<OAuthOptions>>(Options.Create(options));
        }
    }
}
