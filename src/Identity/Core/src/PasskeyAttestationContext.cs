// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the context for passkey attestation.
/// </summary>
public sealed class PasskeyAttestationContext
{
    /// <summary>
    /// Gets or sets the <see cref="Http.HttpContext"/> for the current request. 
    /// </summary>
    public required HttpContext HttpContext { get; init; }

    /// <summary>
    /// Gets or sets the credentials obtained by JSON-serializing the result of the
    /// <c>navigator.credentials.create()</c> JavaScript function.
    /// </summary>
    public required string CredentialJson { get; init; }

    /// <summary>
    /// Gets or sets the state to be used in the attestation procedure.
    /// </summary>
    /// <remarks>
    /// This is expected to match the <see cref="PasskeyCreationOptionsResult.AttestationState"/>
    /// previously returned from <see cref="IPasskeyHandler{TUser}.MakeCreationOptionsAsync(PasskeyUserEntity, HttpContext)"/>.
    /// </remarks>
    public required string? AttestationState { get; init; }
}
