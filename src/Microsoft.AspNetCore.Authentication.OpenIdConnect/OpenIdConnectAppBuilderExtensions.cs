// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to add OpenID Connect authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class OpenIdConnectAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="OpenIdConnectMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables OpenID Connect authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseOpenIdConnectAuthentication(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            return app.UseMiddleware<OpenIdConnectMiddleware>();
        }

        /// <summary>
        /// Adds the <see cref="OpenIdConnectMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables OpenID Connect authentication capabilities.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">A <see cref="OpenIdConnectOptions"/> that specifies options for the middleware.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseOpenIdConnectAuthentication(this IApplicationBuilder app, OpenIdConnectOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<OpenIdConnectMiddleware>(Options.Create(options));
        }
    }
}
