// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cryptography.KeyDerivation.PBKDF2;

namespace Microsoft.AspNet.Cryptography.KeyDerivation
{
    public static class KeyDerivation
    {
        public static byte[] Pbkdf2(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
        {
            // parameter checking
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }
            if (salt == null)
            {
                throw new ArgumentNullException(nameof(salt));
            }
            if (prf < KeyDerivationPrf.Sha1 || prf > KeyDerivationPrf.Sha512)
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
