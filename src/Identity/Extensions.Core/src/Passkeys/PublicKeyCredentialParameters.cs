// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
#if NET10_0_OR_GREATER
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
#else
        [
            new(COSEAlgorithmIdentifier.PS256),
            new(COSEAlgorithmIdentifier.PS384),
            new(COSEAlgorithmIdentifier.PS512),
            new(COSEAlgorithmIdentifier.RS256),
            new(COSEAlgorithmIdentifier.RS384),
            new(COSEAlgorithmIdentifier.RS512),
        ];
#endif

    public PublicKeyCredentialParameters(COSEAlgorithmIdentifier alg)
        : this(type: "public-key", alg)
    {
    }

    public string Type { get; } = type;

    public COSEAlgorithmIdentifier Alg { get; } = alg;
}
