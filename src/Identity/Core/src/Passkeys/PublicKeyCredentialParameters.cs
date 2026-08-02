// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to supply additional parameters when creating a new credential.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialparameters"/>
/// </remarks>
internal readonly struct PublicKeyCredentialParameters()
{

    /// <summary>
    /// Gets the type of the credential.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialparameters-type"/>.
    /// </remarks>
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets the cryptographic signature algorithm identifier.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialparameters-alg"/>.
    /// </remarks>
    public required COSEAlgorithmIdentifier Alg { get; init; }
}
