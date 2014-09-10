// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Security.Twitter;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="TwitterAuthenticationMiddleware"/>
    /// </summary>
    public static class TwitterAuthenticationExtensions
    {
        /// <summary>
        /// Authenticate users using Twitter
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method</param>
        /// <param name="consumerKey">The Twitter-issued consumer key</param>
        /// <param name="consumerSecret">The Twitter-issued consumer secret</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/></returns>
        public static IApplicationBuilder UseTwitterAuthentication([NotNull] this IApplicationBuilder app, [NotNull] string consumerKey, [NotNull] string consumerSecret)
        {
            return app.UseTwitterAuthentication(
                new TwitterAuthenticationOptions
                {
                    ConsumerKey = consumerKey,
                    ConsumerSecret = consumerSecret,
                });
        }

        /// <summary>
        /// Authenticate users using Twitter
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method</param>
        /// <param name="options">Middleware configuration options</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/></returns>
        public static IApplicationBuilder UseTwitterAuthentication([NotNull] this IApplicationBuilder app, [NotNull] TwitterAuthenticationOptions options)
        {
            if (string.IsNullOrEmpty(options.SignInAsAuthenticationType))
            {
                options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            }
            return app.UseMiddleware<TwitterAuthenticationMiddleware>(options);
        }
    }
}
