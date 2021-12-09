// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

/// <summary>
/// Specifies a symmetric encryption algorithm to use for providing confidentiality
/// to protected payloads.
/// </summary>
public enum EncryptionAlgorithm
{
    /// <summary>
    /// The AES algorithm (FIPS 197) with a 128-bit key running in Cipher Block Chaining mode.
    /// </summary>
    AES_128_CBC,

    /// <summary>
    /// The AES algorithm (FIPS 197) with a 192-bit key running in Cipher Block Chaining mode.
    /// </summary>
    AES_192_CBC,

    /// <summary>
    /// The AES algorithm (FIPS 197) with a 256-bit key running in Cipher Block Chaining mode.
    /// </summary>
    AES_256_CBC,

    /// <summary>
    /// The AES algorithm (FIPS 197) with a 128-bit key running in Galois/Counter Mode (FIPS SP 800-38D).
    /// </summary>
    /// <remarks>
    /// This cipher mode produces a 128-bit authentication tag. This algorithm is currently only
    /// supported on Windows.
    /// </remarks>
    AES_128_GCM,

    /// <summary>
    /// The AES algorithm (FIPS 197) with a 192-bit key running in Galois/Counter Mode (FIPS SP 800-38D).
    /// </summary>
    /// <remarks>
    /// This cipher mode produces a 128-bit authentication tag.
    /// </remarks>
    AES_192_GCM,

    /// <summary>
    /// The AES algorithm (FIPS 197) with a 256-bit key running in Galois/Counter Mode (FIPS SP 800-38D).
    /// </summary>
    /// <remarks>
    /// This cipher mode produces a 128-bit authentication tag.
    /// </remarks>
    AES_256_GCM,
}
