// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the client data passed to <c>navigator.credentials.get()</c> or <c>navigator.credentials.create()</c>.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-collectedclientdata"/>
/// </remarks>
internal sealed class CollectedClientData
{
    /// <summary>
    /// Gets or sets the type of the operation that produced the client data.
    /// </summary>
    /// <remarks>
    /// Will be either "webauthn.create" or "webauthn.get".
    /// </remarks>
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets the challenge provided by the relying party.
    /// </summary>
    public required BufferSource Challenge { get; init; }

    /// <summary>
    /// Gets or sets the fully qualified origin of the requester.
    /// </summary>
    public required string Origin { get; init; }

    /// <summary>
    /// Gets or sets whether the credential creation request was initiated from
    /// a different origin than the one associated with the relying party.
    /// </summary>
    public bool? CrossOrigin { get; init; }

    /// <summary>
    /// Gets or sets information about the state of the token binding protocol.
    /// </summary>
    public TokenBinding? TokenBinding { get; init; }
}
