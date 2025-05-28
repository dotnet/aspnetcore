// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the client data passed to <c>navigator.credentials.get()</c> or <c>navigator.credentials.create()</c>.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-collectedclientdata"/>
/// </remarks>
internal sealed class CollectedClientData(string type, BufferSource challenge, string origin)
{
    /// <summary>
    /// Gets the type of the operation that produced the client data.
    /// </summary>
    /// <remarks>
    /// Will be either "webauthn.create" or "webauthn.get".
    /// </remarks>
    public string Type { get; } = type;

    /// <summary>
    /// Gets the challenge provided by the relying party.
    /// </summary>
    public BufferSource Challenge { get; } = challenge;

    /// <summary>
    /// Gets the fully qualified origin of the requester.
    /// </summary>
    public string Origin { get; } = origin;

    /// <summary>
    /// Gets or sets whether the credential creation request was initiated from
    /// a different origin than the one associated with the relying party.
    /// </summary>
    public bool? CrossOrigin { get; set; }

    /// <summary>
    /// Gets or sets information about the state of the token binding protocol.
    /// </summary>
    public TokenBinding? TokenBinding { get; set; }
}
