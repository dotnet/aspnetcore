// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The request type for the "/login" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// The user's email address which acts as a user name.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// The user's password.
    /// </summary>
    public required string Password { get; init; }

    /// <summary>
    /// The optional two-factor authenticator code. This may be required for users who have enabled two-factor authentication.
    /// This is not required if a <see cref="TwoFactorRecoveryCode"/> is sent.
    /// </summary>
    public string? TwoFactorCode { get; init; }

    /// <summary>
    /// An optional two-factor recovery code from <see cref="TwoFactorResponse.RecoveryCodes"/>.
    /// This is required for users who have enabled two-factor authentication but lost access to their <see cref="TwoFactorCode"/>.
    /// </summary>
    public string? TwoFactorRecoveryCode { get; init; }
}
