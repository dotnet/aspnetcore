// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Used to supply additional parameters when creating a new credential.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialparameters"/>
/// </remarks>
[method: JsonConstructor]
internal readonly struct PublicKeyCredentialParameters(string type, COSEAlgorithmIdentifier alg)
{
    /// <summary>
    /// Contains all supported public key credential parameters.
    /// </summary>
    /// <remarks>
    /// Keep this list in sync with the supported algorithms in <see cref="CredentialPublicKey"/>.
    /// This list is sorted in the order of preference, with the most preferred algorithm first.
    /// </remarks>
    internal static IReadOnlyList<PublicKeyCredentialParameters> AllSupportedParameters { get; } =
        [
            new(COSEAlgorithmIdentifier.ES256),
            new(COSEAlgorithmIdentifier.PS256),
            new(COSEAlgorithmIdentifier.ES384),
            new(COSEAlgorithmIdentifier.PS384),
            new(COSEAlgorithmIdentifier.PS512),
            new(COSEAlgorithmIdentifier.RS256),
            new(COSEAlgorithmIdentifier.ES512),
            new(COSEAlgorithmIdentifier.RS384),
            new(COSEAlgorithmIdentifier.RS512),
        ];

    public PublicKeyCredentialParameters(COSEAlgorithmIdentifier alg)
        : this(type: "public-key", alg)
    {
    }

    /// <summary>
    /// Gets the type of the credential.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialparameters-type"/>.
    /// </remarks>
    public string Type { get; } = type;

    /// <summary>
    /// Gets or sets the cryptographic signature algorithm identifier.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-publickeycredentialparameters-alg"/>.
    /// </remarks>
    public COSEAlgorithmIdentifier Alg { get; } = alg;
}
