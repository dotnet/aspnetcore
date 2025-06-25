// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents options for creating a passkey.
/// </summary>
/// <param name="userEntity">The user entity associated with the passkey.</param>
/// <param name="optionsJson">The JSON representation of the options.</param>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialcreationoptions"/>.
/// </remarks>
public sealed class PasskeyCreationOptions(PasskeyUserEntity userEntity, string optionsJson)
{
    private readonly string _optionsJson = optionsJson;

    /// <summary>
    /// Gets the user entity associated with the passkey.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialuserentity"/>.
    /// </remarks>>
    public PasskeyUserEntity UserEntity { get; } = userEntity;

    /// <summary>
    /// Gets the JSON representation of the options.
    /// </summary>
    /// <remarks>
    /// The structure of the JSON string matches the description in the WebAuthn specification.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialcreationoptionsjson"/>.
    /// </remarks>
    public string AsJson()
        => _optionsJson;

    /// <summary>
    /// Gets the JSON representation of the options.
    /// </summary>
    /// <remarks>
    /// The structure of the JSON string matches the description in the WebAuthn specification.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialcreationoptionsjson"/>.
    /// </remarks>
    public override string ToString()
        => _optionsJson;
}
