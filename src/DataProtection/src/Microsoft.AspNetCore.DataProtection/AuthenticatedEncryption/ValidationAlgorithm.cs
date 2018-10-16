// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption
{
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
}
