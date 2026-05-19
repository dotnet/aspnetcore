// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents information about a public key/private key pair.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#typedefdef-publickeycredentialjson" />
/// </remarks>
internal sealed class PublicKeyCredential<TResponse>
    where TResponse : notnull, AuthenticatorResponse
{
    /// <summary>
    /// Gets or sets the credential ID.
    /// </summary>
    public required BufferSource Id { get; init; }

    /// <summary>
    /// Gets the type of the public key credential.
    /// </summary>
    /// <remarks>
    /// This is always expected to have the value <c>"public-key"</c>.
    /// </remarks>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the client extensions map.
    /// </summary>
    public required JsonElement ClientExtensionResults { get; init; }

    /// <summary>
    /// Gets or sets the authenticator response.
    /// </summary>
    public required TResponse Response { get; init; }

    /// <summary>
    /// Gets or sets a string indicating the mechanism by which the WebAuthn implementation
    /// is attached to the authenticator.
    /// </summary>
    public string? AuthenticatorAttachment { get; init; }
}
