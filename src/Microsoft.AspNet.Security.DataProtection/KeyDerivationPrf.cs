// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Security.DataProtection.Cng
{
    /// <summary>
    /// Specifies the PRF which should be used for the key derivation algorithm.
    /// </summary>
    public enum KeyDerivationPrf
    {
        /// <summary>
        /// SHA-1 (FIPS PUB 180-4)
        /// </summary>
        Sha1,

        /// <summary>
        /// SHA-256 (FIPS PUB 180-4)
        /// </summary>
        Sha256,

        /// <summary>
        /// SHA-512 (FIPS PUB 180-4)
        /// </summary>
        Sha512,
    }
}
