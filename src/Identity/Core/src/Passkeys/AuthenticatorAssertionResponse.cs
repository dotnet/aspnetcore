// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the response returned by an authenticator during the assertion phase of a WebAuthn login
/// process.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#authenticatorassertionresponse"/>.
/// </remarks>
internal sealed class AuthenticatorAssertionResponse : AuthenticatorResponse
{
    /// <summary>
    /// Gets or sets the authenticator data.
    /// </summary>
    public required BufferSource AuthenticatorData { get; init; }

    /// <summary>
    /// Gets or sets the assertion signature.
    /// </summary>
    public required BufferSource Signature { get; init; }

    /// <summary>
    /// Gets or sets the opaque user identifier.
    /// </summary>
    public BufferSource? UserHandle { get; init; }
}
