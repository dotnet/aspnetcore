// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.Cng
{
    public class CbcAuthenticatedEncryptorTests
    {
        [ConditionalFact]
        [ConditionalRunTestOnlyOnWindows]
        public void Encrypt_Decrypt_RoundTrips()
        {
            // Arrange
            Secret kdk = new Secret(new byte[512 / 8]);
            CbcAuthenticatedEncryptor encryptor = new CbcAuthenticatedEncryptor(kdk,
                symmetricAlgorithmHandle: CachedAlgorithmHandles.AES_CBC,
                symmetricAlgorithmKeySizeInBytes: 256 / 8,
                hmacAlgorithmHandle: CachedAlgorithmHandles.HMAC_SHA256);
            ArraySegment<byte> plaintext = new ArraySegment<byte>(Encoding.UTF8.GetBytes("plaintext"));
            ArraySegment<byte> aad = new ArraySegment<byte>(Encoding.UTF8.GetBytes("aad"));

            // Act
            byte[] ciphertext = encryptor.Encrypt(plaintext, aad);
            byte[] decipheredtext = encryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

            // Assert
            Assert.Equal(plaintext, decipheredtext);
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyOnWindows]
        public void Encrypt_Decrypt_Tampering_Fails()
        {
            // Arrange
            Secret kdk = new Secret(new byte[512 / 8]);
            CbcAuthenticatedEncryptor encryptor = new CbcAuthenticatedEncryptor(kdk,
                symmetricAlgorithmHandle: CachedAlgorithmHandles.AES_CBC,
                symmetricAlgorithmKeySizeInBytes: 256 / 8,
                hmacAlgorithmHandle: CachedAlgorithmHandles.HMAC_SHA256);
            ArraySegment<byte> plaintext = new ArraySegment<byte>(Encoding.UTF8.GetBytes("plaintext"));
            ArraySegment<byte> aad = new ArraySegment<byte>(Encoding.UTF8.GetBytes("aad"));
            byte[] validCiphertext = encryptor.Encrypt(plaintext, aad);

            // Act & assert - 1
            // Ciphertext is too short to be a valid payload
            byte[] invalidCiphertext_tooShort = new byte[10];
            Assert.Throws<CryptographicException>(() =>
            {
                encryptor.Decrypt(new ArraySegment<byte>(invalidCiphertext_tooShort), aad);
            });

            // Act & assert - 2
            // Ciphertext has been manipulated
            byte[] invalidCiphertext_manipulated = (byte[])validCiphertext.Clone();
            invalidCiphertext_manipulated[0] ^= 0x01;
            Assert.Throws<CryptographicException>(() =>
            {
                encryptor.Decrypt(new ArraySegment<byte>(invalidCiphertext_manipulated), aad);
            });

            // Act & assert - 3
            // Ciphertext is too long
            byte[] invalidCiphertext_tooLong = validCiphertext.Concat(new byte[] { 0 }).ToArray();
            Assert.Throws<CryptographicException>(() =>
            {
                encryptor.Decrypt(new ArraySegment<byte>(invalidCiphertext_tooLong), aad);
            });

            // Act & assert - 4
            // AAD is incorrect
            Assert.Throws<CryptographicException>(() =>
            {
                encryptor.Decrypt(new ArraySegment<byte>(validCiphertext), new ArraySegment<byte>(Encoding.UTF8.GetBytes("different aad")));
            });
        }

        [ConditionalFact]
        [ConditionalRunTestOnlyOnWindows]
        public void Encrypt_KnownKey()
        {
            // Arrange
            Secret kdk = new Secret(Encoding.UTF8.GetBytes("master key"));
            CbcAuthenticatedEncryptor encryptor = new CbcAuthenticatedEncryptor(kdk,
                symmetricAlgorithmHandle: CachedAlgorithmHandles.AES_CBC,
                symmetricAlgorithmKeySizeInBytes: 256 / 8,
                hmacAlgorithmHandle: CachedAlgorithmHandles.HMAC_SHA256,
                genRandom: new SequentialGenRandom());
            ArraySegment<byte> plaintext = new ArraySegment<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, 2, 3);
            ArraySegment<byte> aad = new ArraySegment<byte>(new byte[] { 7, 6, 5, 4, 3, 2, 1, 0 }, 1, 4);

            // Act
            byte[] retVal = encryptor.Encrypt(
                plaintext: plaintext,
                additionalAuthenticatedData: aad,
                preBufferSize: 3,
                postBufferSize: 4);

            // Assert

            // retVal := 00 00 00 (preBuffer)
            //         | 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F (keyModifier)
            //         | 10 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F (IV)
            //         | B7 EA 3E 32 58 93 A3 06 03 89 C6 66 03 63 08 4B (encryptedData)
            //         | 9D 8A 85 C7 0F BD 98 D8 7F 72 E7 72 3E B5 A6 26 (HMAC)
            //         | 6C 38 77 F7 66 19 A2 C9 2C BB AD DA E7 62 00 00
            //         | 00 00 00 00 (postBuffer)

            string retValAsString = Convert.ToBase64String(retVal);
            Assert.Equal("AAAAAAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh+36j4yWJOjBgOJxmYDYwhLnYqFxw+9mNh/cudyPrWmJmw4d/dmGaLJLLut2udiAAAAAAAA", retValAsString);
        }
    }
}
