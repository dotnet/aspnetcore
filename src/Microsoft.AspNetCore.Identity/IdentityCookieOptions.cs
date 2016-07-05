// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Represents all the options you can use to configure the cookies middleware uesd by the identity system.
    /// </summary>
    public class IdentityCookieOptions
    {
        private static readonly string CookiePrefix = "Identity";
        private static readonly string DefaultApplicationScheme = CookiePrefix + ".Application";
        private static readonly string DefaultExternalScheme = CookiePrefix + ".External";
        private static readonly string DefaultTwoFactorRememberMeScheme = CookiePrefix + ".TwoFactorRememberMe";
        private static readonly string DefaultTwoFactorUserIdScheme = CookiePrefix + ".TwoFactorUserId";

        /// <summary>
        /// Constructs a new instance of <see cref="IdentityCookieOptions"/>.
        /// </summary>
        public IdentityCookieOptions()
        {
            // Configure all of the cookie middlewares
            ApplicationCookie = new CookieAuthenticationOptions
            {
                AuthenticationScheme = DefaultApplicationScheme,
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                LoginPath = new PathString("/Account/Login"),
                Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
                }
            };

            ExternalCookie = new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = false,
                AuthenticationScheme = DefaultExternalScheme,
                CookieName = DefaultExternalScheme,
                ExpireTimeSpan = TimeSpan.FromMinutes(5)
            };

            TwoFactorRememberMeCookie = new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = false,
                AuthenticationScheme = DefaultTwoFactorRememberMeScheme,
                CookieName = DefaultTwoFactorRememberMeScheme
            };

            TwoFactorUserIdCookie = new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = false,
                AuthenticationScheme = DefaultTwoFactorUserIdScheme,
                CookieName = DefaultTwoFactorUserIdScheme,
                ExpireTimeSpan = TimeSpan.FromMinutes(5)
            };
        }

        /// <summary>
        /// The options for the application cookie.
        /// </summary>
        public CookieAuthenticationOptions ApplicationCookie { get; set; }

        /// <summary>
        /// The options for the external cookie.
        /// </summary>
        public CookieAuthenticationOptions ExternalCookie { get; set; }

        /// <summary>
        /// The options for the two factor remember me cookie.
        /// </summary>
        public CookieAuthenticationOptions TwoFactorRememberMeCookie { get; set; }

        /// <summary>
        /// The options for the two factor user id cookie.
        /// </summary>
        public CookieAuthenticationOptions TwoFactorUserIdCookie { get; set; }

        /// <summary>
        /// Gets the scheme used to identify application authentication cookies.
        /// </summary>
        /// <value>The scheme used to identify application authentication cookies.</value>
        public string ApplicationCookieAuthenticationScheme => ApplicationCookie?.AuthenticationScheme;

        /// <summary>
        /// Gets the scheme used to identify external authentication cookies.
        /// </summary>
        /// <value>The scheme used to identify external authentication cookies.</value>
        public string ExternalCookieAuthenticationScheme => ExternalCookie?.AuthenticationScheme;

        /// <summary>
        /// Gets the scheme used to identify Two Factor authentication cookies for round tripping user identities.
        /// </summary>
        /// <value>The scheme used to identify user identity 2fa authentication cookies.</value>
        public string TwoFactorUserIdCookieAuthenticationScheme => TwoFactorUserIdCookie?.AuthenticationScheme;

        /// <summary>
        /// Gets the scheme used to identify Two Factor authentication cookies for saving the Remember Me state.
        /// </summary>
        /// <value>The scheme used to identify remember me application authentication cookies.</value>        
        public string TwoFactorRememberMeCookieAuthenticationScheme => TwoFactorRememberMeCookie?.AuthenticationScheme;
    }
}
