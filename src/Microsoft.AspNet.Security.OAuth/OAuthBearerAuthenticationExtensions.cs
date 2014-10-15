// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.OAuth;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods to add OAuth Bearer authentication capabilities to an HTTP application pipeline
    /// </summary>
    public static class OAuthBearerAuthenticationExtensions
    {
        public static IServiceCollection ConfigureOAuthBearerAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<OAuthBearerAuthenticationOptions> configure)
        {
            return services.ConfigureOptions(configure);
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
        /// <param name="options">Options which control the processing of the bearer header.</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseOAuthBearerAuthentication([NotNull] this IApplicationBuilder app, Action<OAuthBearerAuthenticationOptions> configureOptions = null, string optionsName = "")
        {
            return app.UseMiddleware<OAuthBearerAuthenticationMiddleware>(
                new ConfigureOptions<OAuthBearerAuthenticationOptions>(configureOptions ?? (o => { }))
                {
                    Name = optionsName
                });
        }
    }
}
