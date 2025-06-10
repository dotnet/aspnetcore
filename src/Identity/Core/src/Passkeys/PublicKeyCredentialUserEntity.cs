// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to supply additional user account attributes when creating a new credential.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialuserentityjson"/>.
/// </remarks>
internal sealed class PublicKeyCredentialUserEntity
{
    /// <summary>
    /// Gets or sets the user handle of the user account.
    /// </summary>
    public required BufferSource Id { get; init; }

    /// <summary>
    /// Gets or sets the human-palatable name for the entity.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the human-palatable name for the user account, intended only for display.
    /// </summary>
    public required string DisplayName { get; init; }
}
