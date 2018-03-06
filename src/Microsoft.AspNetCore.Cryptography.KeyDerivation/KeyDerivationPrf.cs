// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation
{
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
}
