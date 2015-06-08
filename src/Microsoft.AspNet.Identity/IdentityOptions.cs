// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Represents all the options you can used to configure the identity system.
    /// </summary>
    public class IdentityOptions
    {
        /// <summary>
        /// Gets or sets the <see cref="ClaimsIdentityOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="ClaimsIdentityOptions"/> for the identity system.
        /// </value>
        public ClaimsIdentityOptions ClaimsIdentity { get; set; } = new ClaimsIdentityOptions();

        /// <summary>
        /// Gets or sets the <see cref="UserOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="UserOptions"/> for the identity system.
        /// </value>
        public UserOptions User { get; set; } = new UserOptions();

        /// <summary>
        /// Gets or sets the <see cref="PasswordOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="PasswordOptions"/> for the identity system.
        /// </value>
        public PasswordOptions Password { get; set; } = new PasswordOptions();

        /// <summary>
        /// Gets or sets the <see cref="LockoutOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="LockoutOptions"/> for the identity system.
        /// </value>
        public LockoutOptions Lockout { get; set; } = new LockoutOptions();

        /// <summary>
        /// Gets or sets the <see cref="SignInOptions"/> for the identity system.
        /// </summary>
        /// <value>
        /// The <see cref="SignInOptions"/> for the identity system.
        /// </value>
        public SignInOptions SignIn { get; set; } = new SignInOptions();

        /// <summary>
        /// Gets or sets the <see cref="TimeSpan"/> after which security stamps are re-validated.
        /// </summary>
        /// <value>
        /// The <see cref="TimeSpan"/> after which security stamps are re-validated.
        /// </value>
        public TimeSpan SecurityStampValidationInterval { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Gets or sets the <see cref="EmailConfirmationTokenProvider"/> used to generate tokens used in account confirmation emails.
        /// </summary>
        /// <value>
        /// The <see cref="EmailConfirmationTokenProvider"/> used to generate tokens used in account confirmation emails.
        /// </value>
        public string EmailConfirmationTokenProvider { get; set; } = Resources.DefaultTokenProvider;

        /// <summary>
        /// Gets or sets the <see cref="PasswordResetTokenProvider"/> used to generate tokens used in password reset emails.
        /// </summary>
        /// <value>
        /// The <see cref="PasswordResetTokenProvider"/> used to generate tokens used in password reset emails.
        /// </value>
        public string PasswordResetTokenProvider { get; set; } = Resources.DefaultTokenProvider;

        /// <summary>
        /// Gets or sets the <see cref="ChangeEmailTokenProvider"/> used to generate tokens used in email change confirmation emails.
        /// </summary>
        /// <value>
        /// The <see cref="ChangeEmailTokenProvider"/> used to generate tokens used in email change confirmation emails.
        /// </value>
        public string ChangeEmailTokenProvider { get; set; } = Resources.DefaultTokenProvider;

        /// <summary>
        /// Gets or sets the scheme used to identify application authentication cookies.
        /// </summary>
        /// <value>The scheme used to identify application authentication cookies.</value>
        public static string ApplicationCookieAuthenticationScheme { get; set; } = typeof(IdentityOptions).Namespace + ".Application";

        /// <summary>
        /// Gets or sets the scheme used to identify external authentication cookies.
        /// </summary>
        /// <value>The scheme used to identify external authentication cookies.</value>
        public static string ExternalCookieAuthenticationScheme { get; set; } = typeof(IdentityOptions).Namespace + ".External";

        /// <summary>
        /// Gets or sets the scheme used to identify Two Factor authentication cookies for round tripping user identities.
        /// </summary>
        /// <value>The scheme used to identify user identity 2fa authentication cookies.</value>
        public static string TwoFactorUserIdCookieAuthenticationScheme { get; set; } = typeof(IdentityOptions).Namespace + ".TwoFactorUserId";
        
        /// <summary>
        /// Gets or sets the scheme used to identify Two Factor authentication cookies for saving the Remember Me state.
        /// </summary>
        /// <value>The scheme used to identify remember me application authentication cookies.</value>        
        public static string TwoFactorRememberMeCookieAuthenticationScheme { get; set; } = typeof(IdentityOptions).Namespace + ".TwoFactorRememberMe";


        /// <summary>
        /// Gets or sets the authentication type used when constructing an <see cref="ClaimsIdentity"/> from an application cookie.
        /// </summary>
        /// <value>The authentication type used when constructing an <see cref="ClaimsIdentity"/> from an application cookie.</value>
        public static string ApplicationCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".Application.AuthType";

        /// <summary>
        /// Gets or sets the authentication type used when constructing an <see cref="ClaimsIdentity"/> from an external identity cookie.
        /// </summary>
        /// <value>The authentication type used when constructing an <see cref="ClaimsIdentity"/> from an external identity cookie.</value>
        public static string ExternalCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".External.AuthType";

        /// <summary>
        /// Gets or sets the authentication type used when constructing an <see cref="ClaimsIdentity"/> from an two factor authentication cookie.
        /// </summary>
        /// <value>The authentication type used when constructing an <see cref="ClaimsIdentity"/> from an two factor authentication cookie.</value>
        public static string TwoFactorUserIdCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".TwoFactorUserId.AuthType";

        /// <summary>
        /// Gets or sets the authentication type used when constructing an <see cref="ClaimsIdentity"/> from an two factor remember me authentication cookie.
        /// </summary>
        /// <value>The authentication type used when constructing an <see cref="ClaimsIdentity"/> from an two factor remember me authentication cookie.</value>
        public static string TwoFactorRememberMeCookieAuthenticationType { get; set; } = typeof(IdentityOptions).Namespace + ".TwoFactorRemeberMe.AuthType";
    }
}