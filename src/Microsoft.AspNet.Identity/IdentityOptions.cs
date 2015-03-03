// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        public static string ApplicationCookieAuthenticationScheme { get; set; } = typeof(IdentityOptions).Namespace + ".Application";
        public static string ExternalCookieAuthenticationScheme { get; set; } = typeof(IdentityOptions).Namespace + ".External";
        public static string TwoFactorUserIdCookieAuthenticationScheme { get; set; } = typeof(IdentityOptions).Namespace + ".TwoFactorUserId";
        public static string TwoFactorRememberMeCookieAuthenticationScheme { get; set; } = typeof(IdentityOptions).Namespace + ".TwoFactorRemeberMe";

        public static string ApplicationCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".Application.AuthType";
        public static string ExternalCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".External.AuthType";
        public static string TwoFactorUserIdCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".TwoFactorUserId.AuthType";
        public static string TwoFactorRememberMeCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".TwoFactorRemeberMe.AuthType";

    }
}