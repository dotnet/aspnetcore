// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Identity.Authentication;
using Microsoft.AspNet.Security.Cookies;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Startup extensions
    /// </summary>
    public static class BuilderExtensions
    {
        public static IBuilder UseTwoFactorSignInCookies(this IBuilder builder)
        {
            // TODO: expose some way for them to customize these cookie lifetimes?
            builder.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = HttpAuthenticationManager.TwoFactorRememberedAuthenticationType,
                AuthenticationMode = Security.AuthenticationMode.Passive
            });
            builder.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = HttpAuthenticationManager.TwoFactorUserIdAuthenticationType,
                AuthenticationMode = Security.AuthenticationMode.Passive
            });
            return builder;
        }
    }
}