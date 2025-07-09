// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents arguments for generating <see cref="PasskeyCreationOptions"/>.
/// </summary>
/// <param name="userEntity">The passkey user entity.</param>
public sealed class PasskeyCreationArgs(PasskeyUserEntity userEntity)
{
    /// <summary>
    /// Gets the passkey user entity.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialuserentity"/>.
    /// </remarks>
    public PasskeyUserEntity UserEntity { get; } = userEntity;

    /// <summary>
    /// Gets or sets the authenticator selection criteria.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-authenticatorselectioncriteria"/>.
    /// </remarks>
    public AuthenticatorSelectionCriteria? AuthenticatorSelection { get; set; }

    /// <summary>
    /// Gets or sets the attestation conveyance preference.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#enumdef-attestationconveyancepreference"/>.
    /// The default value is "none".
    /// </remarks>
    public string Attestation { get; set; } = "none";

    /// <summary>
    /// Gets or sets the client extension inputs.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialcreationoptions-extensions"/>.
    /// </remarks>
    public JsonElement? Extensions { get; set; }
}
