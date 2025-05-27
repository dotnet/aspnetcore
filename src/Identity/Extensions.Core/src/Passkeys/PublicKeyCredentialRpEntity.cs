// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to supply Relying Party attributes when creating a new credential.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialrpentity"/>.
/// </remarks>
/// <param name="name"></param>
internal sealed class PublicKeyCredentialRpEntity(string name)
{
    /// <summary>
    /// Gets the human-palatable name for the entity.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets or sets the unique identifier for the replying party entity.
    /// </summary>
    public string? Id { get; set; }
}
