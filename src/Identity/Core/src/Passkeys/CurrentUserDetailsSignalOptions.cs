// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the information needed to signal the current details of a user.
/// </summary>
/// <remarks>
/// These options are accepted by the WebAuthn <c>signalCurrentUserDetails</c> method.
/// See <see href="https://www.w3.org/TR/webauthn-3/#sctn-signal-methods"/>.
/// </remarks>
internal sealed class CurrentUserDetailsSignalOptions
{
    /// <summary>
    /// Gets the relying party identifier.
    /// </summary>
    public required string RpId { get; init; }

    /// <summary>
    /// Gets the user handle of the user that owns the credentials.
    /// </summary>
    public required BufferSource UserId { get; init; }

    /// <summary>
    /// Gets the name of the user.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the display name of the user.
    /// </summary>
    public required string DisplayName { get; init; }
}
