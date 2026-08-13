// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the information needed to signal the current state of a user's known passkeys to authenticators.
/// </summary>
/// <remarks>
/// This is a superset of the options accepted by the WebAuthn <c>signalAllAcceptedCredentials</c>
/// and <c>signalCurrentUserDetails</c> methods.
/// See <see href="https://www.w3.org/TR/webauthn-3/#sctn-signal-methods"/>.
/// </remarks>
internal sealed class KnownPasskeysSignalOptions
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
    /// Gets the credential IDs that are currently registered for the user.
    /// </summary>
    public required IReadOnlyList<BufferSource> AllAcceptedCredentialIds { get; init; }

    /// <summary>
    /// Gets the name of the user.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the display name of the user.
    /// </summary>
    public required string DisplayName { get; init; }
}
