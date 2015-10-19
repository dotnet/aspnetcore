// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.JwtBearer;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods to add OpenIdConnect Bearer authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class JwtBearerAppBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="JwtBearerMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Bearer token processing capabilities.
        /// This middleware understands appropriately
        /// formatted and secured tokens which appear in the request header. If the Options.AuthenticationMode is Active, the
        /// claims within the bearer token are added to the current request's IPrincipal User. If the Options.AuthenticationMode 
        /// is Passive, then the current request is not modified, but IAuthenticationManager AuthenticateAsync may be used at
        /// any time to obtain the claims from the request's bearer token.
        /// See also http://tools.ietf.org/html/rfc6749
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="options">A <see cref="JwtBearerOptions"/> that specifies options for the middleware.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseJwtBearerAuthentication(this IApplicationBuilder app, JwtBearerOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<JwtBearerMiddleware>(options);
        }

        /// <summary>
        /// Adds the <see cref="JwtBearerMiddleware"/> middleware to the specified <see cref="IApplicationBuilder"/>, which enables Bearer token processing capabilities.
        /// This middleware understands appropriately
        /// formatted and secured tokens which appear in the request header. If the Options.AuthenticationMode is Active, the
        /// claims within the bearer token are added to the current request's IPrincipal User. If the Options.AuthenticationMode 
        /// is Passive, then the current request is not modified, but IAuthenticationManager AuthenticateAsync may be used at
        /// any time to obtain the claims from the request's bearer token.
        /// See also http://tools.ietf.org/html/rfc6749
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
        /// <param name="configureOptions">An action delegate to configure the provided <see cref="JwtBearerOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseJwtBearerAuthentication(this IApplicationBuilder app, Action<JwtBearerOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var options = new JwtBearerOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseJwtBearerAuthentication(options);
        }
    }
}
