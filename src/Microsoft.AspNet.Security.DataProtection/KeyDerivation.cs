// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Security.DataProtection.PBKDF2;

namespace Microsoft.AspNet.Security.DataProtection
{
    public static class KeyDerivation
    {
        public static byte[] Pbkdf2(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
        {
            // parameter checking
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            if (salt == null)
            {
                throw new ArgumentNullException("salt");
            }
            if (prf < KeyDerivationPrf.Sha1 || prf > KeyDerivationPrf.Sha512)
            {
                throw new ArgumentOutOfRangeException("prf");
            }
            if (iterationCount <= 0)
            {
                throw new ArgumentOutOfRangeException("iterationCount");
            }
            if (numBytesRequested <= 0)
            {
                throw new ArgumentOutOfRangeException("numBytesRequested");
            }

            return Pbkdf2Util.Pbkdf2Provider.DeriveKey(password, salt, prf, iterationCount, numBytesRequested);
        }
    }
}
