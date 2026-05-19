// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the response returned by an authenticator during the attestation phase of a WebAuthn registration
/// process.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#authenticatorattestationresponse" />.
/// </remarks>
internal sealed class AuthenticatorAttestationResponse : AuthenticatorResponse
{
    /// <summary>
    /// Gets or sets the attestation object.
    /// </summary>
    public required BufferSource AttestationObject { get; init; }

    /// <summary>
    /// Gets or sets the strings describing which transport methods (e.g., usb, nfc) are believed
    /// to be supported with the authenticator.
    /// </summary>
    /// <remarks>
    /// May be empty or <c>null</c> if the information is not available.
    /// </remarks>
    public string[]? Transports { get; init; } = [];
}
