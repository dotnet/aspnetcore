// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

/// <summary>
/// Specifies a message authentication algorithm to use for providing tamper-proofing
/// to protected payloads.
/// </summary>
public enum ValidationAlgorithm
{
    /// <summary>
    /// The HMAC algorithm (RFC 2104) using the SHA-256 hash function (FIPS 180-4).
    /// </summary>
    HMACSHA256,

    /// <summary>
    /// The HMAC algorithm (RFC 2104) using the SHA-512 hash function (FIPS 180-4).
    /// </summary>
    HMACSHA512,
}
