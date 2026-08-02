// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The response type for the "/manage/2fa" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// </summary>
public sealed class TwoFactorResponse
{
    /// <summary>
    /// The shared key generally for TOTP authenticator apps that is usually presented to the user as a QR code.
    /// </summary>
    public required string SharedKey { get; init; }

    /// <summary>
    /// The number of unused <see cref="RecoveryCodes"/> remaining.
    /// </summary>
    public required int RecoveryCodesLeft { get; init; }

    /// <summary>
    /// The recovery codes to use if the <see cref="SharedKey"/> is lost. This will be omitted from the response unless
    /// <see cref="TwoFactorRequest.ResetRecoveryCodes"/> was set or two-factor was enabled for the first time.
    /// </summary>
    public string[]? RecoveryCodes { get; init; }

    /// <summary>
    /// Whether or not two-factor login is required for the current authenticated user.
    /// </summary>
    public required bool IsTwoFactorEnabled { get; init; }

    /// <summary>
    /// Whether or not the current client has been remembered by two-factor authentication cookies. This is always <see langword="false"/> for non-cookie authentication schemes.
    /// </summary>
    public required bool IsMachineRemembered { get; init; }
}
