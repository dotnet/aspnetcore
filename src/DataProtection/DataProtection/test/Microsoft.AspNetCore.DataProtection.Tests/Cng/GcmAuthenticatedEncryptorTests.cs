// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.Cng;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.DataProtection.Cng;

public class GcmAuthenticatedEncryptorTests
{
    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void Encrypt_Decrypt_RoundTrips()
    {
        // Arrange
        Secret kdk = new Secret(new byte[512 / 8]);
        CngGcmAuthenticatedEncryptor encryptor = new CngGcmAuthenticatedEncryptor(kdk, CachedAlgorithmHandles.AES_GCM, symmetricAlgorithmKeySizeInBytes: 256 / 8);
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
        CngGcmAuthenticatedEncryptor encryptor = new CngGcmAuthenticatedEncryptor(kdk, CachedAlgorithmHandles.AES_GCM, symmetricAlgorithmKeySizeInBytes: 256 / 8);
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
        CngGcmAuthenticatedEncryptor encryptor = new CngGcmAuthenticatedEncryptor(kdk, CachedAlgorithmHandles.AES_GCM, symmetricAlgorithmKeySizeInBytes: 128 / 8, genRandom: new SequentialGenRandom());
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
        //         | 10 11 12 13 14 15 16 17 18 19 1A 1B (nonce)
        //         | 43 B6 91 (encryptedData)
        //         | 8D 0D 66 D9 A1 D9 44 2D 5D 8E 41 DA 39 60 9C E8 (authTag)
        //         | 00 00 00 00 (postBuffer)

        string retValAsString = Convert.ToBase64String(retVal);
        Assert.Equal("AAAAAAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaG0O2kY0NZtmh2UQtXY5B2jlgnOgAAAAA", retValAsString);
    }
}
