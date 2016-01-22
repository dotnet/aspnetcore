// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Identity extensions for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Enables ASP.NET identity for the current application.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance this method extends.</returns>
        public static IApplicationBuilder UseIdentity(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var marker = app.ApplicationServices.GetService<IdentityMarkerService>();
            if (marker == null)
            {
                throw new InvalidOperationException(Resources.MustCallAddIdentity);
            }

            var options = app.ApplicationServices.GetRequiredService<IOptions<IdentityOptions>>().Value;
            app.UseCookieAuthentication(options.Cookies.ExternalCookie);
            app.UseCookieAuthentication(options.Cookies.TwoFactorRememberMeCookie);
            app.UseCookieAuthentication(options.Cookies.TwoFactorUserIdCookie);
            app.UseCookieAuthentication(options.Cookies.ApplicationCookie);
            return app;
        }
    }
}