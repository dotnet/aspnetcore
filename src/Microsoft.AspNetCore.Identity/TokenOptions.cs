// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Identity
{
    public class TokenOptions
    {
        public static readonly string DefaultProvider = "Default";
        public static readonly string DefaultEmailProvider = "Email";
        public static readonly string DefaultPhoneProvider = "Phone";

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
    }
}