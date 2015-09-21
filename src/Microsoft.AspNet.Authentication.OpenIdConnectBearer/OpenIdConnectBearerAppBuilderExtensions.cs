// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.OpenIdConnectBearer;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods to add OpenIdConnect Bearer authentication capabilities to an HTTP application pipeline
    /// </summary>
    public static class OpenIdConnectBearerAppBuilderExtensions
    {
        /// <summary>
        /// Adds Bearer token processing to an HTTP application pipeline. This middleware understands appropriately
        /// formatted and secured tokens which appear in the request header. If the Options.AuthenticationMode is Active, the
        /// claims within the bearer token are added to the current request's IPrincipal User. If the Options.AuthenticationMode 
        /// is Passive, then the current request is not modified, but IAuthenticationManager AuthenticateAsync may be used at
        /// any time to obtain the claims from the request's bearer token.
        /// See also http://tools.ietf.org/html/rfc6749
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="options">Options which control the processing of the bearer header.</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseOpenIdConnectBearerAuthentication([NotNull] this IApplicationBuilder app, [NotNull] OpenIdConnectBearerOptions options)
        {
            return app.UseMiddleware<OpenIdConnectBearerMiddleware>(options);
        }

        /// <summary>
        /// Adds Bearer token processing to an HTTP application pipeline. This middleware understands appropriately
        /// formatted and secured tokens which appear in the request header. If the Options.AuthenticationMode is Active, the
        /// claims within the bearer token are added to the current request's IPrincipal User. If the Options.AuthenticationMode 
        /// is Passive, then the current request is not modified, but IAuthenticationManager AuthenticateAsync may be used at
        /// any time to obtain the claims from the request's bearer token.
        /// See also http://tools.ietf.org/html/rfc6749
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="configureOptions">Used to configure Middleware options.</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseOpenIdConnectBearerAuthentication([NotNull] this IApplicationBuilder app, Action<OpenIdConnectBearerOptions> configureOptions)
        {
            var options = new OpenIdConnectBearerOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseOpenIdConnectBearerAuthentication(options);
        }
    }
}
