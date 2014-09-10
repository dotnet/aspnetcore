// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Security.Facebook;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="FacebookAuthenticationMiddleware"/>.
    /// </summary>
    public static class FacebookAuthenticationExtensions
    {
        /// <summary>
        /// Authenticate users using Facebook.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="appId">The appId assigned by Facebook.</param>
        /// <param name="appSecret">The appSecret assigned by Facebook.</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseFacebookAuthentication([NotNull] this IApplicationBuilder app, [NotNull] string appId, [NotNull] string appSecret)
        {
            return app.UseFacebookAuthentication(new FacebookAuthenticationOptions()
            {
                AppId = appId,
                AppSecret = appSecret,
            });
        }

        /// <summary>
        /// Authenticate users using Facebook.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="options">The middleware configuration options.</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseFacebookAuthentication([NotNull] this IApplicationBuilder app, [NotNull] FacebookAuthenticationOptions options)
        {
            if (string.IsNullOrEmpty(options.SignInAsAuthenticationType))
            {
                options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            }
            return app.UseMiddleware<FacebookAuthenticationMiddleware>(options);
        }
    }
}
