// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Identity;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Startup extensions
    /// </summary>
    public static class BuilderExtensions
    {
        public static IApplicationBuilder UseIdentity(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }
            app.UseCookieAuthentication(null, IdentityOptions.ExternalCookieAuthenticationType);
            app.UseCookieAuthentication(null, IdentityOptions.ApplicationCookieAuthenticationType);
            app.UseCookieAuthentication(null, IdentityOptions.TwoFactorRememberMeCookieAuthenticationType);
            app.UseCookieAuthentication(null, IdentityOptions.TwoFactorUserIdCookieAuthenticationType);
            app.UseCookieAuthentication(null, IdentityOptions.ApplicationCookieAuthenticationType);
            return app;
        }
    }
}