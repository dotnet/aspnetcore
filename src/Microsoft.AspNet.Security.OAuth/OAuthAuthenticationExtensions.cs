// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNet.Security.OAuth;
using Microsoft.AspNet.Security.Infrastructure;
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
        public static IApplicationBuilder UseOAuthAuthentication([NotNull] this IApplicationBuilder app, [NotNull] string authenticationType, Action<OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>> configureOptions = null)
        {
            return app.UseMiddleware<OAuthAuthenticationMiddleware<OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>, IOAuthAuthenticationNotifications>>(
                new ConfigureOptions<OAuthAuthenticationOptions<IOAuthAuthenticationNotifications>>(options =>
                {
                    options.AuthenticationType = authenticationType;
                    options.Caption = authenticationType;
                    if (configureOptions != null)
                    {
                        configureOptions(options);
                    }
                }) 
                {
                    Name = authenticationType,
                });
        }
    }
}
