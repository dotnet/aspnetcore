// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Identity;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.DependencyInjection;

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
            var options = app.ApplicationServices.GetService<IOptionsAccessor<IdentityOptions>>().Options;
            app.UseCookieAuthentication(options.ApplicationCookie);
            app.SetDefaultSignInAsAuthenticationType(options.DefaultSignInAsAuthenticationType);
            app.UseCookieAuthentication(options.ExternalCookie);
            app.UseCookieAuthentication(options.TwoFactorRememberMeCookie);
            app.UseCookieAuthentication(options.TwoFactorUserIdCookie);
            return app;
        }
    }
}