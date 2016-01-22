// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2;

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation
{
    /// <summary>
    /// Provides algorithms for performing key derivation.
    /// </summary>
    public static class KeyDerivation
    {
        /// <summary>
        /// Performs key derivation using the PBKDF2 algorithm.
        /// </summary>
        /// <param name="password">The password from which to derive the key.</param>
        /// <param name="salt">The salt to be used during the key derivation process.</param>
        /// <param name="prf">The pseudo-random function to be used in the key derivation process.</param>
        /// <param name="iterationCount">The number of iterations of the pseudo-random function to apply
        /// during the key derivation process.</param>
        /// <param name="numBytesRequested">The desired length (in bytes) of the derived key.</param>
        /// <returns>The derived key.</returns>
        /// <remarks>
        /// The PBKDF2 algorithm is specified in RFC 2898.
        /// </remarks>
        public static byte[] Pbkdf2(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            if (salt == null)
            {
                throw new ArgumentNullException(nameof(salt));
            }

            // parameter checking
            if (prf < KeyDerivationPrf.HMACSHA1 || prf > KeyDerivationPrf.HMACSHA512)
            {
                throw new ArgumentOutOfRangeException(nameof(prf));
            }
            if (iterationCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(iterationCount));
            }
            if (numBytesRequested <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numBytesRequested));
            }

            return Pbkdf2Util.Pbkdf2Provider.DeriveKey(password, salt, prf, iterationCount, numBytesRequested);
        }
    }
}
