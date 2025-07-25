// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Contains the context for passkey attestation statement verification.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#verification-procedure"/>.
/// </remarks>
public sealed class PasskeyAttestationStatementVerificationContext
{
    /// <summary>
    /// Gets or sets the <see cref="HttpContext"/> for the current request.
    /// </summary>
    public required HttpContext HttpContext { get; init; }

    /// <summary>
    /// Gets or sets the attestation object as a byte array.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#attestation-object"/>.
    /// </remarks>
    public required ReadOnlyMemory<byte> AttestationObject { get; init; }

    /// <summary>
    /// Gets or sets the hash of the client data as a byte array.
    /// </summary>
    public required ReadOnlyMemory<byte> ClientDataHash { get; init; }
}
