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

        public string ChangeEmailTokenProvider { get; set; } = Resources.DefaultTokenProvider;

        public static string ApplicationCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".Application";
        public static string ExternalCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".External";
        public static string TwoFactorUserIdCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".TwoFactorUserId";
        public static string TwoFactorRememberMeCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".TwoFactorRemeberMe";
    }
}