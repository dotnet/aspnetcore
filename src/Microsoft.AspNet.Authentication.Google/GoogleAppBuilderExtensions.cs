// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Google;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="GoogleMiddleware"/>.
    /// </summary>
    public static class GoogleAppBuilderExtensions
    {
        /// <summary>
        /// Authenticate users using Google OAuth 2.0.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> passed to the configure method.</param>
        /// <param name="configureOptions">Used to configure Middleware options.</param>
        /// <param name="optionsName">Name of the options instance to be used</param>
        /// <returns>The updated <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseGoogleAuthentication([NotNull] this IApplicationBuilder app, Action<GoogleOptions> configureOptions = null, string optionsName = "")
        {
            return app.UseMiddleware<GoogleMiddleware>(
                 new ConfigureOptions<GoogleOptions>(configureOptions ?? (o => { })));
        }
    }
}