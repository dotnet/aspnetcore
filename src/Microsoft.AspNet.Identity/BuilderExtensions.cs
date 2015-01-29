// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

using Microsoft.AspNet.Identity;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Identity extensions for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Enables the ASP.NET identity for the current application.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance this method extends.</returns>
        public static IApplicationBuilder UseIdentity(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            app.UseCookieAuthentication(null, IdentityOptions.ExternalCookieAuthenticationType);
            app.UseCookieAuthentication(null, IdentityOptions.TwoFactorRememberMeCookieAuthenticationType);
            app.UseCookieAuthentication(null, IdentityOptions.TwoFactorUserIdCookieAuthenticationType);
            app.UseCookieAuthentication(null, IdentityOptions.ApplicationCookieAuthenticationType);
            return app;
        }
    }
}