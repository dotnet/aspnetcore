// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

internal static class AuthenticatedEncryptorExtensions
{
    public static byte[] Encrypt(this IAuthenticatedEncryptor encryptor, ArraySegment<byte> plaintext, ArraySegment<byte> additionalAuthenticatedData, uint preBufferSize, uint postBufferSize)
    {
        // Can we call the optimized version?
        var optimizedEncryptor = encryptor as IOptimizedAuthenticatedEncryptor;
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
            var temp = encryptor.Encrypt(plaintext, additionalAuthenticatedData);
            var retVal = new byte[checked(preBufferSize + temp.Length + postBufferSize)];
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
        var plaintextAsGuid = Guid.NewGuid();
        var plaintextAsBytes = plaintextAsGuid.ToByteArray();
        var aad = Guid.NewGuid().ToByteArray();

        // Act
        var protectedData = encryptor.Encrypt(new ArraySegment<byte>(plaintextAsBytes), new ArraySegment<byte>(aad));
        var roundTrippedData = encryptor.Decrypt(new ArraySegment<byte>(protectedData), new ArraySegment<byte>(aad));

        // Assert
        CryptoUtil.Assert(roundTrippedData != null && roundTrippedData.Length == plaintextAsBytes.Length && plaintextAsGuid == new Guid(roundTrippedData),
            "Plaintext did not round-trip properly through the authenticated encryptor.");
    }
}
