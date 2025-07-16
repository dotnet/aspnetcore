// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents options for requesting a credential.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialrequestoptionsjson"/>
/// </remarks>
internal sealed class PublicKeyCredentialRequestOptions
{
    /// <summary>
    /// Gets or sets the challenge that the authenticator signs when producing an assertion for the requested credential.
    /// </summary>
    public required BufferSource Challenge { get; init; }

    /// <summary>
    /// Gets or sets a time in milliseconds that the server is willing to wait for the call to complete.
    /// </summary>
    public ulong? Timeout { get; init; }

    /// <summary>
    /// Gets or sets the relying party identifier.
    /// </summary>
    public string? RpId { get; init; }

    /// <summary>
    /// Gets or sets the credentials of the identified user account, if any.
    /// </summary>
    public IReadOnlyList<PublicKeyCredentialDescriptor> AllowCredentials { get; init; } = [];

    /// <summary>
    /// Gets or sets the user verification requirement for the request.
    /// </summary>
    public string UserVerification { get; init; } = "preferred";

    /// <summary>
    /// Gets or sets hints that guide the user agent in interacting with the user.
    /// </summary>
    public IReadOnlyList<string> Hints { get; init; } = [];

    /// <summary>
    /// Gets or sets the client extension inputs that the relying party supports.
    /// </summary>
    public JsonElement? Extensions { get; init; }
}
