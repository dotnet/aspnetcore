// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to supply Relying Party attributes when creating a new credential.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialrpentity"/>.
/// </remarks>
internal sealed class PublicKeyCredentialRpEntity
{
    /// <summary>
    /// Gets or sets the human-palatable name for the entity.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the unique identifier for the replying party entity.
    /// </summary>
    public string? Id { get; init; }
}
