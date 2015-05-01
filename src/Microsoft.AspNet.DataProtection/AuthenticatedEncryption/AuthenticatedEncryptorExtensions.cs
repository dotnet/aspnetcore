// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Cryptography;

namespace Microsoft.AspNet.DataProtection.AuthenticatedEncryption
{
    internal static class AuthenticatedEncryptorExtensions
    {
        public static byte[] Encrypt(this IAuthenticatedEncryptor encryptor, ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize)
        {
            // Can we call the optimized version?
            IOptimizedAuthenticatedEncryptor optimizedEncryptor = encryptor as IOptimizedAuthenticatedEncryptor;
            if (optimizedEncryptor != null)
            {
                return optimizedEncryptor.Encrypt(plaintext, additionalAuthenticatedData, preBufferSize, postBufferSize);
            }

            // Fall back to the unoptimized version
            if (preBufferSize == 0 && postBufferSize == 0)
            {
                // optimization: call through to inner encryptor with no modifications
                return encryptor.Encrypt(plaintext, additionalAuthenticatedData);
            }
            else
            {
                byte[] temp = encryptor.Encrypt(plaintext, additionalAuthenticatedData);
                byte[] retVal = new byte[checked(preBufferSize + temp.Length + postBufferSize)];
                Buffer.BlockCopy(temp, 0, retVal, checked((int)preBufferSize), temp.Length);
                return retVal;
            }
        }

        /// <summary>
        /// Performs a self-test of this encryptor by running a sample payload through an
        /// encrypt-then-decrypt operation. Throws if the operation fails.
        /// </summary>
        public static void PerformSelfTest(this IAuthenticatedEncryptor encryptor)
        {
            // Arrange
            Guid plaintextAsGuid = Guid.NewGuid();
            byte[] plaintextAsBytes = plaintextAsGuid.ToByteArray();
            byte[] aad = Guid.NewGuid().ToByteArray();

            // Act
            byte[] protectedData = encryptor.Encrypt(new ArraySegment<byte>(plaintextAsBytes), new ArraySegment<byte>(aad));
            byte[] roundTrippedData = encryptor.Decrypt(new ArraySegment<byte>(protectedData), new ArraySegment<byte>(aad));

            // Assert
            CryptoUtil.Assert(roundTrippedData != null && roundTrippedData.Length == plaintextAsBytes.Length && plaintextAsGuid == new Guid(roundTrippedData),
                "Plaintext did not round-trip properly through the authenticated encryptor.");
        }
    }
}
