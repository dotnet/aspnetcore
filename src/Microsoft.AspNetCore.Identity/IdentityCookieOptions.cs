// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Represents all the options you can use to configure the cookies middleware uesd by the identity system.
    /// </summary>
    public class IdentityCookieOptions
    {
        private static readonly string CookiePrefix = "Identity";
        /// <summary>
        /// The scheme used to identify application authentication cookies.
        /// </summary>
        public static readonly string ApplicationScheme = CookiePrefix + ".Application";

        /// <summary>
        /// The scheme used to identify external authentication cookies.
        /// </summary>
        public static readonly string ExternalScheme = CookiePrefix + ".External";

        /// <summary>
        /// The scheme used to identify Two Factor authentication cookies for saving the Remember Me state.
        /// </summary>
        public static readonly string TwoFactorRememberMeScheme = CookiePrefix + ".TwoFactorRememberMe";

        /// <summary>
        /// The scheme used to identify Two Factor authentication cookies for round tripping user identities.
        /// </summary>
        public static readonly string TwoFactorUserIdScheme = CookiePrefix + ".TwoFactorUserId";

        /// <summary>
        /// The options for the application cookie.
        /// </summary>
        [Obsolete("See https://go.microsoft.com/fwlink/?linkid=845470", error: true)]
        public CookieAuthenticationOptions ApplicationCookie { get; set; }

        /// <summary>
        /// The options for the external cookie.
        /// </summary>
        [Obsolete("See https://go.microsoft.com/fwlink/?linkid=845470", error: true)]
        public CookieAuthenticationOptions ExternalCookie { get; set; }

        /// <summary>
        /// The options for the two factor remember me cookie.
        /// </summary>
        [Obsolete("See https://go.microsoft.com/fwlink/?linkid=845470", error: true)]
        public CookieAuthenticationOptions TwoFactorRememberMeCookie { get; set; }

        /// <summary>
        /// The options for the two factor user id cookie.
        /// </summary>
        [Obsolete("See https://go.microsoft.com/fwlink/?linkid=845470", error: true)]
        public CookieAuthenticationOptions TwoFactorUserIdCookie { get; set; }

        /// <summary>
        /// Gets the scheme used to identify application authentication cookies.
        /// </summary>
        /// <value>The scheme used to identify application authentication cookies.</value>
        public string ApplicationCookieAuthenticationScheme { get; set; } = ApplicationScheme;

        /// <summary>
        /// Gets the scheme used to identify external authentication cookies.
        /// </summary>
        /// <value>The scheme used to identify external authentication cookies.</value>
        public string ExternalCookieAuthenticationScheme { get; set; } = ExternalScheme;

        /// <summary>
        /// Gets the scheme used to identify Two Factor authentication cookies for round tripping user identities.
        /// </summary>
        /// <value>The scheme used to identify user identity 2fa authentication cookies.</value>
        public string TwoFactorUserIdCookieAuthenticationScheme { get; set; } = TwoFactorUserIdScheme;

        /// <summary>
        /// Gets the scheme used to identify Two Factor authentication cookies for saving the Remember Me state.
        /// </summary>
        /// <value>The scheme used to identify remember me application authentication cookies.</value>        
        public string TwoFactorRememberMeCookieAuthenticationScheme { get; set; } = TwoFactorRememberMeScheme;
    }
}
