// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Used to configure identity cookie options.
    /// </summary>
    public class IdentityCookiesBuilder
    {
        /// <summary>
        /// Used to configure the application cookie.
        /// </summary>
        public OptionsBuilder<CookieAuthenticationOptions> ApplicationCookie { get; set; }

        /// <summary>
        /// Used to configure the external cookie.
        /// </summary>
        public OptionsBuilder<CookieAuthenticationOptions> ExternalCookie { get; set; }

        /// <summary>
        /// Used to configure the two factor remember me cookie.
        /// </summary>
        public OptionsBuilder<CookieAuthenticationOptions> TwoFactorRememberMeCookie { get; set; }

        /// <summary>
        /// Used to configure the two factor user id cookie.
        /// </summary>
        public OptionsBuilder<CookieAuthenticationOptions> TwoFactorUserIdCookie { get; set; }
    }
}
