// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to supply additional user account attributes when creating a new credential.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialuserentityjson"/>.
/// </remarks>
internal sealed class PublicKeyCredentialUserEntity(BufferSource id, string name, string displayName)
{
    /// <summary>
    /// Gets the user handle of the user account.
    /// </summary>
    public BufferSource Id { get; } = id;

    /// <summary>
    /// Gets the human-palatable name for the entity.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the human-palatable name for the user account, intended only for display.
    /// </summary>
    public string DisplayName { get; } = displayName;
}
