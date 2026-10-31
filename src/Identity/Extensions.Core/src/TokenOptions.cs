// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Identity;

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
    /// Default token provider name used by the email provider.
    /// </summary>
    public static readonly string DefaultEmailProvider = "Email";

    /// <summary>
    /// Default token provider name used by the phone provider.
    /// </summary>
    public static readonly string DefaultPhoneProvider = "Phone";

    /// <summary>
    /// Default token provider name used by the <see cref="AuthenticatorTokenProvider"/>.
    /// </summary>
    public static readonly string DefaultAuthenticatorProvider = "Authenticator";

    /// <summary>
    /// Will be used to construct UserTokenProviders with the key used as the providerName.
    /// </summary>
    public Dictionary<string, TokenProviderDescriptor> ProviderMap { get; set; } = new Dictionary<string, TokenProviderDescriptor>();

    /// <summary>
    /// Gets or sets the token provider used to generate tokens used in account confirmation emails.
    /// </summary>
    /// <value>
    /// The <see cref="IUserTwoFactorTokenProvider{TUser}"/> used to generate tokens used in account confirmation emails.
    /// </value>
    public string EmailConfirmationTokenProvider { get; set; } = DefaultProvider;

    /// <summary>
    /// Gets or sets the <see cref="IUserTwoFactorTokenProvider{TUser}"/> used to generate tokens used in password reset emails.
    /// </summary>
    /// <value>
    /// The <see cref="IUserTwoFactorTokenProvider{TUser}"/> used to generate tokens used in password reset emails.
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
    /// Gets or sets the <see cref="ChangePhoneNumberTokenProvider"/> used to generate tokens used when changing phone numbers.
    /// </summary>
    /// <value>
    /// The <see cref="ChangePhoneNumberTokenProvider"/> used to generate tokens used when changing phone numbers.
    /// </value>
    public string ChangePhoneNumberTokenProvider { get; set; } = DefaultPhoneProvider;

    /// <summary>
    /// Gets or sets the <see cref="AuthenticatorTokenProvider"/> used to validate two factor sign ins with an authenticator.
    /// </summary>
    /// <value>
    /// The <see cref="AuthenticatorTokenProvider"/> used to validate two factor sign ins with an authenticator.
    /// </value>
    public string AuthenticatorTokenProvider { get; set; } = DefaultAuthenticatorProvider;

    /// <summary>
    /// Gets or sets the issuer used for the authenticator issuer.
    /// </summary>
    public string AuthenticatorIssuer { get; set; } = "Microsoft.AspNetCore.Identity.UI";
}
