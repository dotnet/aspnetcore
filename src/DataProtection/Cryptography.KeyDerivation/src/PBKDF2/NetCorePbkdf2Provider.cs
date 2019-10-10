// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2
{
    /// <summary>
    /// Implements Pbkdf2 using <see cref="Rfc2898DeriveBytes"/>.
    /// </summary>
    internal sealed class NetCorePbkdf2Provider : IPbkdf2Provider
    {
        private static readonly ManagedPbkdf2Provider _fallbackProvider = new ManagedPbkdf2Provider();

        public byte[] DeriveKey(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
        {
            Debug.Assert(password != null);
            Debug.Assert(salt != null);
            Debug.Assert(iterationCount > 0);
            Debug.Assert(numBytesRequested > 0);

            if (salt.Length < 8)
            {
                // Rfc2898DeriveBytes enforces the 8 byte recommendation.
                // To maintain compatibility, we call into ManagedPbkdf2Provider for salts shorter than 8 bytes
                // because we can't use Rfc2898DeriveBytes with this salt.
                return _fallbackProvider.DeriveKey(password, salt, prf, iterationCount, numBytesRequested);
            }
            else
            {
                return DeriveKeyImpl(password, salt, prf, iterationCount, numBytesRequested);
            }
        }

        private static byte[] DeriveKeyImpl(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested)
        {
            HashAlgorithmName algorithmName;
            switch (prf)
            {
                case KeyDerivationPrf.HMACSHA1:
                    algorithmName = HashAlgorithmName.SHA1;
                    break;
                case KeyDerivationPrf.HMACSHA256:
                    algorithmName = HashAlgorithmName.SHA256;
                    break;
                case KeyDerivationPrf.HMACSHA512:
                    algorithmName = HashAlgorithmName.SHA512;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            using (var rfc = new Rfc2898DeriveBytes(passwordBytes, salt, iterationCount, algorithmName))
            {
                return rfc.GetBytes(numBytesRequested);
            }
        }
    }
}
#endif
