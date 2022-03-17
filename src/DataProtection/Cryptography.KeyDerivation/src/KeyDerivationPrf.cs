// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation;

/// <summary>
/// Specifies the PRF which should be used for the key derivation algorithm.
/// </summary>
public enum KeyDerivationPrf
{
    /// <summary>
    /// The HMAC algorithm (RFC 2104) using the SHA-1 hash function (FIPS 180-4).
    /// </summary>
    HMACSHA1,

    /// <summary>
    /// The HMAC algorithm (RFC 2104) using the SHA-256 hash function (FIPS 180-4).
    /// </summary>
    HMACSHA256,

    /// <summary>
    /// The HMAC algorithm (RFC 2104) using the SHA-512 hash function (FIPS 180-4).
    /// </summary>
    HMACSHA512,
}
