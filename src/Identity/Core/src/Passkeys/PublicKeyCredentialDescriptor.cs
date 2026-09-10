// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Identifies a specific public key credential.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialdescriptorjson"/>
/// </remarks>
internal sealed class PublicKeyCredentialDescriptor
{
    /// <summary>
    /// Gets or sets the type of the public key credential.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets the identifier of the public key credential.
    /// </summary>
    public required BufferSource Id { get; init; }

    /// <summary>
    /// Gets or sets hints as to how the client might communicate with the authenticator.
    /// </summary>
    public IReadOnlyList<string> Transports { get; init; } = [];
}
