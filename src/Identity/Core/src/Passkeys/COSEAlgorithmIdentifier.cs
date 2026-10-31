// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents a number identifying a cryptographic algorithm.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#typedefdef-cosealgorithmidentifier"/>.
/// </remarks>
internal enum COSEAlgorithmIdentifier : int
{
    RS1 = -65535,
    RS512 = -259,
    RS384 = -258,
    RS256 = -257,
    PS512 = -39,
    PS384 = -38,
    PS256 = -37,
    ES512 = -36,
    ES384 = -35,
    EdDSA = -8,
    ES256 = -7,
    ES256K = -47,
}
