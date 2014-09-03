// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Security.OAuth;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="OAuthAuthenticationMiddleware"/>
    /// </summary>
    public static class OAuthAuthenticationExtensions
    {
        /// <summary>
        /// Authenticate users using OAuth.
        /// </summary>
        /// <param name="app">The <see cref="IBuilder"/> passed to the configure method.</param>
        /// <param name="options">The middleware configuration options.</param>
        /// <returns>The updated <see cref="IBuilder"/>.</returns>
        public static IBuilder UseOAuthAuthentication([NotNull] this IBuilder app, [NotNull] OAuthAuthenticationOptions<IOAuthAuthenticationNotifications> options)
        {
            if (string.IsNullOrEmpty(options.SignInAsAuthenticationType))
            {
                options.SignInAsAuthenticationType = app.GetDefaultSignInAsAuthenticationType();
            }
            if (options.Notifications == null)
            {
                options.Notifications = new OAuthAuthenticationNotifications();
            }
            return app.UseMiddleware<OAuthAuthenticationMiddleware<OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>, IOAuthAuthenticationNotifications>>(options);
        }
    }
}
