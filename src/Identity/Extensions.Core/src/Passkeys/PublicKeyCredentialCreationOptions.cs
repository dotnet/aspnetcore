// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents options for credential creation.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialcreationoptionsjson"/>.
/// </remarks>
internal sealed class PublicKeyCredentialCreationOptions(
    PublicKeyCredentialRpEntity rp,
    PublicKeyCredentialUserEntity user,
    BufferSource challenge)
{
    /// <summary>
    /// Gets the name and identifier for the relying party requesting attestation.
    /// </summary>
    public PublicKeyCredentialRpEntity Rp { get; } = rp;

    /// <summary>
    /// Gets the names and and identifier for the user account performing the registration.
    /// </summary>
    public PublicKeyCredentialUserEntity User { get; } = user;

    /// <summary>
    /// Gets a challenge that the authenticator signs when producing an attestation object for the newly created credential.
    /// </summary>
    public BufferSource Challenge { get; } = challenge;

    /// <summary>
    /// Gets the key types and signature algorithms the relying party supports, ordered from most preferred to least preferred.
    /// </summary>
    public IReadOnlyList<PublicKeyCredentialParameters> PubKeyCredParams { get; set; } = [];

    /// <summary>
    /// Gets or sets the time, in milliseconds, that the relying party is willing to wait for the call to complete.
    /// </summary>
    public ulong? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the existing credentials mapped to the user account.
    /// </summary>
    public IReadOnlyList<PublicKeyCredentialDescriptor> ExcludeCredentials { get; set; } = [];

    /// <summary>
    /// Gets or sets settings that the authenticator should satisfy when creating a new credential.
    /// </summary>
    public AuthenticatorSelectionCriteria? AuthenticatorSelection { get; set; }

    /// <summary>
    /// Gets or sets hints that guide the user agent in interacting with the user.
    /// </summary>
    public IReadOnlyList<string> Hints { get; set; } = [];

    /// <summary>
    /// Gets or sets the attestation conveyance preference for the relying party.
    /// </summary>
    public string Attestation { get; set; } = "none";

    /// <summary>
    /// Gets or sets the attestation statement format preferences of the relying party, ordered from most preferred to least preferred.
    /// </summary>
    public IReadOnlyList<string> AttestationFormats { get; set; } = [];

    /// <summary>
    /// Gets or sets the client extension inputs that the relying party supports.
    /// </summary>
    public JsonElement? Extensions { get; set; }
}
