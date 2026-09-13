// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the information needed to signal that a credential is unknown to the server.
/// </summary>
/// <remarks>
/// These options are accepted by the WebAuthn <c>signalUnknownCredential</c> method.
/// See <see href="https://www.w3.org/TR/webauthn-3/#sctn-signal-methods"/>.
/// </remarks>
internal sealed class UnknownCredentialSignalOptions
{
    /// <summary>
    /// Gets the relying party identifier.
    /// </summary>
    public required string RpId { get; init; }

    /// <summary>
    /// Gets the credential ID that is unknown to the server.
    /// </summary>
    public required BufferSource CredentialId { get; init; }
}
