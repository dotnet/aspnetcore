// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Security.Google;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="GoogleAuthenticationMiddleware"/>.
    /// </summary>
    public static class GoogleAuthenticationExtensions
    {
        /// <summary>
        /// Authenticate users using Google OAuth 2.0.
        /// </summary>
        /// <param name="app">The <see cref="IBuilder"/> passed to the configure method.</param>
        /// <param name="clientId">The google assigned client id.</param>
        /// <param name="clientSecret">The google assigned client secret.</param>
        /// <returns>The updated <see cref="IBuilder"/>.</returns>
        public static IBuilder UseGoogleAuthentication([NotNull] this IBuilder app, [NotNull] string clientId, [NotNull] string clientSecret)
        {
            return app.UseGoogleAuthentication(
                new GoogleAuthenticationOptions
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                });
        }

        /// <summary>
        /// Authenticate users using Google OAuth 2.0.
        /// </summary>
        /// <param name="app">The <see cref="IBuilder"/> passed to the configure method.</param>
        /// <param name="options">Middleware configuration options.</param>
        /// <returns>The updated <see cref="IBuilder"/>.</returns>
        public static IBuilder UseGoogleAuthentication([NotNull] this IBuilder app, [NotNull] GoogleAuthenticationOptions options)
        {
            if (string.IsNullOrEmpty(options.SignInAsAuthenticationType))
            {
                options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            }
            return app.UseMiddleware<GoogleAuthenticationMiddleware>(options);
        }
    }
}