// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Security.Google;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using System;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="GoogleAuthenticationMiddleware"/>.
    /// </summary>
    public static class GoogleAuthenticationExtensions
    {
        public static IServiceCollection ConfigureGoogleAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<GoogleAuthenticationOptions> configure)
        {
            return services.Configure(configure);
        }

        /// <summary>
        /// Authenticate users using Google OAuth 2.0.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="configureOptions">Used to configure Middleware options.</param>
        /// <param name="optionsName">Name of the options instance to be used</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseGoogleAuthentication([NotNull] this IApplicationBuilder app, Action<GoogleAuthenticationOptions> configureOptions = null, string optionsName = "")
        {
            return app.UseMiddleware<GoogleAuthenticationMiddleware>(
                 new ConfigureOptions<GoogleAuthenticationOptions>(configureOptions ?? (o => { }))
                 {
                     Name = optionsName
                 });
        }
    }
}