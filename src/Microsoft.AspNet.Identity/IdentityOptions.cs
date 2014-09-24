// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security;
using Microsoft.AspNet.Security.Cookies;
using System;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Configuration for identity
    /// </summary>
    public class IdentityOptions
    {
        public ClaimsIdentityOptions ClaimsIdentity { get; set; } = new ClaimsIdentityOptions();

        public UserOptions User { get; set; } = new UserOptions();

        public PasswordOptions Password { get; set; } = new PasswordOptions();

        public LockoutOptions Lockout { get; set; } = new LockoutOptions();

        public SignInOptions SignIn { get; set; } = new SignInOptions();

        public TimeSpan SecurityStampValidationInterval { get; set; } = TimeSpan.FromMinutes(30);

        public string EmailConfirmationTokenProvider { get; set; } = Resources.DefaultTokenProvider;

        public string PasswordResetTokenProvider { get; set; } = Resources.DefaultTokenProvider;

        //public string ApplicationCookieAuthenticationType { get; set; }
        //public string ExternalCookieAuthenticationType { get; set; }
        //public string TwoFactorCookieAuthenticationType { get; set; }
        //public string TwoFactorFactorCookieAuthenticationType { get; set; }

        public CookieAuthenticationOptions ApplicationCookie { get; set; } = new CookieAuthenticationOptions
        {
            AuthenticationType = ClaimsIdentityOptions.DefaultAuthenticationType,
            LoginPath = new PathString("/Account/Login"),
            Notifications = new CookieAuthenticationNotifications
            {
                OnValidateIdentity = SecurityStampValidator.ValidateIdentityAsync
            }
        };

        // Move to setups for named per cookie option

        public string DefaultSignInAsAuthenticationType { get; set; } = ClaimsIdentityOptions.DefaultExternalLoginAuthenticationType;

        public CookieAuthenticationOptions ExternalCookie { get; set; } = new CookieAuthenticationOptions
        {
            AuthenticationType = ClaimsIdentityOptions.DefaultExternalLoginAuthenticationType,
            AuthenticationMode = AuthenticationMode.Passive
        };

        public CookieAuthenticationOptions TwoFactorRememberMeCookie { get; set; } = new CookieAuthenticationOptions
        {
            AuthenticationType = ClaimsIdentityOptions.DefaultTwoFactorRememberMeAuthenticationType,
            AuthenticationMode = AuthenticationMode.Passive
        };

        public CookieAuthenticationOptions TwoFactorUserIdCookie { get; set; } = new CookieAuthenticationOptions
        {
            AuthenticationType = ClaimsIdentityOptions.DefaultTwoFactorUserIdAuthenticationType,
            AuthenticationMode = AuthenticationMode.Passive

        };
    }
}