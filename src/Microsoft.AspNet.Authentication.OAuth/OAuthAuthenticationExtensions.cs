// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Authentication.OAuth;
using Microsoft.AspNet.Authentication;
using Microsoft.Framework.OptionsModel;

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
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="options">The middleware configuration options.</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseOAuthAuthentication([NotNull] this IApplicationBuilder app, [NotNull] string authenticationScheme, Action<OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>> configureOptions = null)
        {
            return app.UseMiddleware<OAuthAuthenticationMiddleware<OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>, IOAuthAuthenticationNotifications>>(
                new ConfigureOptions<OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>>(options =>
                {
                    options.AuthenticationScheme = authenticationScheme;
                    options.Caption = authenticationScheme;
                    if (configureOptions != null)
                    {
                        configureOptions(options);
                    }
                    if (options.Notifications == null)
                    {
                        options.Notifications = new OAuthAuthenticationNotifications();
                    }
                }) 
                {
                    Name = authenticationScheme,
                });
        }
    }
}
