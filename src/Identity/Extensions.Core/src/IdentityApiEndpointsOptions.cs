// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Defines endpoint route names for Identity API operations.
/// </summary>
public class IdentityApiEndpointsOptions
{
    /// <summary>
    /// Gets or sets the endpoint route name for user registration.
    /// </summary>
    public string Register { get; set; } = "register";

    /// <summary>
    /// Gets or sets the endpoint route name for user login.
    /// </summary>
    public string Login { get; set; } = "login";

    /// <summary>
    /// Gets or sets the endpoint route name for refreshing tokens.
    /// </summary>
    public string Refresh { get; set; } = "refresh";

    /// <summary>
    /// Gets or sets the endpoint route name for confirming email.
    /// </summary>
    public string ConfirmEmail { get; set; } = "confirmEmail";

    /// <summary>
    /// Gets or sets the endpoint route name for resending confirmation email.
    /// </summary>
    public string ResendConfirmationEmail { get; set; } = "resendConfirmationEmail";

    /// <summary>
    /// Gets or sets the endpoint route name for forgot password.
    /// </summary>
    public string ForgotPassword { get; set; } = "forgotPassword";

    /// <summary>
    /// Gets or sets the endpoint route name for resetting password.
    /// </summary>
    public string ResetPassword { get; set; } = "resetPassword";

    /// <summary>
    /// Gets or sets the endpoint route name for two-factor authentication.
    /// </summary>
    public string TwoFactorAuth { get; set; } = "2fa";

    /// <summary>
    /// Gets or sets the endpoint route name for user information.
    /// </summary>
    public string Info { get; set; } = "info";
}
