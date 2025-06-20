// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents options for a passkey request.
/// </summary>
/// <param name="userId">The ID of the user for whom this request is made.</param>
/// <param name="optionsJson">The JSON representation of the options.</param>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialrequestoptions"/>.
/// </remarks>
public sealed class PasskeyRequestOptions(string? userId, string optionsJson)
{
    private readonly string _optionsJson = optionsJson;

    /// <summary>
    /// Gets the ID of the user for whom this request is made.
    /// </summary>
    public string? UserId { get; } = userId;

    /// <summary>
    /// Gets the JSON representation of the options.
    /// </summary>
    /// <remarks>
    /// The structure of the JSON string matches the description in the WebAuthn specification.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialrequestoptionsjson"/>.
    /// </remarks>
    public string AsJson()
        => _optionsJson;

    /// <summary>
    /// Gets the JSON representation of the options.
    /// </summary>
    /// <remarks>
    /// The structure of the JSON string matches the description in the WebAuthn specification.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialrequestoptionsjson"/>.
    /// </remarks>
    public override string ToString()
        => _optionsJson;
}
