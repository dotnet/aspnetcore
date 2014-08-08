// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Security.Facebook;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="FacebookAuthenticationMiddleware"/>
    /// </summary>
    public static class FacebookAuthenticationExtensions
    {
        /// <summary>
        /// Authenticate users using Facebook
        /// </summary>
        /// <param name="app">The <see cref="IBuilder"/> passed to the configure method</param>
        /// <param name="appId">The appId assigned by Facebook</param>
        /// <param name="appSecret">The appSecret assigned by Facebook</param>
        /// <returns>The updated <see cref="IBuilder"/></returns>
        public static IBuilder UseFacebookAuthentication([NotNull] this IBuilder app, [NotNull] string appId, [NotNull] string appSecret)
        {
            return app.UseFacebookAuthentication(new FacebookAuthenticationOptions()
            {
                AppId = appId,
                AppSecret = appSecret,
            });
        }

        /// <summary>
        /// Authenticate users using Facebook
        /// </summary>
        /// <param name="app">The <see cref="IBuilder"/> passed to the configure method</param>
        /// <param name="options">Middleware configuration options</param>
        /// <returns>The updated <see cref="IBuilder"/></returns>
        public static IBuilder UseFacebookAuthentication([NotNull] this IBuilder app, [NotNull] FacebookAuthenticationOptions options)
        {
            if (string.IsNullOrEmpty(options.SignInAsAuthenticationType))
            {
                options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            }
            return app.UseMiddleware<FacebookAuthenticationMiddleware>(options);
        }
    }
}
