// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the information needed to signal the credentials that are currently registered for a user.
/// </summary>
/// <remarks>
/// These options are accepted by the WebAuthn <c>signalAllAcceptedCredentials</c> method.
/// See <see href="https://www.w3.org/TR/webauthn-3/#sctn-signal-methods"/>.
/// </remarks>
internal sealed class AllAcceptedCredentialsSignalOptions
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
}
