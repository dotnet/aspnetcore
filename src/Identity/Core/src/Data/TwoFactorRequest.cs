// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The request type for the "/manage/2fa" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// </summary>
public sealed class TwoFactorRequest
{
    /// <summary>
    /// An optional <see cref="bool"/> to enable or disable the two-factor login requirement for the authenticated user. If null or unset,
    /// the current two-factor login requirement for the user will remain unchanged.
    /// </summary>
    public bool? Enable { get; init; }

    /// <summary>
    /// The two-factor code derived from the <see cref="TwoFactorResponse.SharedKey"/>. This is only required if <see cref="Enable"/> is set to <see langword="true"/>.
    /// </summary>
    public string? TwoFactorCode { get; init; }

    /// <summary>
    /// An optional <see cref="bool"/> to reset the <see cref="TwoFactorResponse.SharedKey"/> to a new random value if <see langword="true"/>.
    /// This automatically disables the two-factor login requirement for the authenticated user until it is re-enabled by a later request.
    /// </summary>
    public bool ResetSharedKey { get; init; }

    /// <summary>
    /// An optional <see cref="bool"/> to reset the <see cref="TwoFactorResponse.RecoveryCodes"/> to new random values if <see langword="true"/>.
    /// <see cref="TwoFactorResponse.RecoveryCodes"/> will be empty unless they are reset or two-factor was enabled for the first time.
    /// </summary>
    public bool ResetRecoveryCodes { get; init; }

    /// <summary>
    /// An optional <see cref="bool"/> to clear the cookie "remember me flag" if present. This has no impact on non-cookie authentication schemes.
    /// </summary>
    public bool ForgetMachine { get; init; }
}
