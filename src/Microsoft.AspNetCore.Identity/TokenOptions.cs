// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Options for user tokens.
    /// </summary>
    public class TokenOptions
    {
        /// <summary>
        /// Default token provider name used by email confirmation, password reset, and change email.
        /// </summary>
        public static readonly string DefaultProvider = "Default";

        /// <summary>
        /// Default token provider name used by the <see cref="EmailTokenProvider{TUser}"/>.
        /// </summary>
        public static readonly string DefaultEmailProvider = "Email";

        /// <summary>
        /// Default token provider name used by the <see cref="PhoneNumberTokenProvider{TUser}"/>.
        /// </summary>
        public static readonly string DefaultPhoneProvider = "Phone";

        /// <summary>
        /// Default token provider name used by the <see cref="AuthenticatorTokenProvider{TUser}"/>.
        /// </summary>
        public static readonly string DefaultAuthenticatorProvider = "Authenticator";

        /// <summary>
        /// Will be used to construct UserTokenProviders with the key used as the providerName.
        /// </summary>
        public Dictionary<string, TokenProviderDescriptor> ProviderMap { get; set; } = new Dictionary<string, TokenProviderDescriptor>();

        /// <summary>
        /// Gets or sets the <see cref="EmailConfirmationTokenProvider"/> used to generate tokens used in account confirmation emails.
        /// </summary>
        /// <value>
        /// The <see cref="EmailConfirmationTokenProvider"/> used to generate tokens used in account confirmation emails.
        /// </value>
        public string EmailConfirmationTokenProvider { get; set; } = DefaultProvider;

        /// <summary>
        /// Gets or sets the <see cref="PasswordResetTokenProvider"/> used to generate tokens used in password reset emails.
        /// </summary>
        /// <value>
        /// The <see cref="PasswordResetTokenProvider"/> used to generate tokens used in password reset emails.
        /// </value>
        public string PasswordResetTokenProvider { get; set; } = DefaultProvider;

        /// <summary>
        /// Gets or sets the <see cref="ChangeEmailTokenProvider"/> used to generate tokens used in email change confirmation emails.
        /// </summary>
        /// <value>
        /// The <see cref="ChangeEmailTokenProvider"/> used to generate tokens used in email change confirmation emails.
        /// </value>
        public string ChangeEmailTokenProvider { get; set; } = DefaultProvider;

        /// <summary>
        /// Gets or sets the <see cref="AuthenticatorTokenProvider"/> used to validate two factor sign ins with an authenticator.
        /// </summary>
        /// <value>
        /// The <see cref="AuthenticatorTokenProvider"/> used to validate two factor sign ins with an authenticator.
        /// </value>
        public string AuthenticatorTokenProvider { get; set; } = DefaultAuthenticatorProvider;
    }
}