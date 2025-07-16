// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.Managed;

namespace Microsoft.AspNetCore.DataProtection.Tests.Aes;
public class AesAuthenticatedEncryptorTests
{
    [Theory]
    [InlineData(128)]
    [InlineData(192)]
    [InlineData(256)]
    public void Roundtrip_AesGcm_TryEncryptDecrypt_CorrectlyEstimatesDataLength(int symmetricKeySizeBits)
    {
        Secret kdk = new Secret(new byte[512 / 8]);
        IAuthenticatedEncryptor encryptor = new AesGcmAuthenticatedEncryptor(kdk, derivedKeySizeInBytes: symmetricKeySizeBits / 8);
        ArraySegment<byte> plaintext = new ArraySegment<byte>(Encoding.UTF8.GetBytes("plaintext"));
        ArraySegment<byte> aad = new ArraySegment<byte>(Encoding.UTF8.GetBytes("aad"));

        var expectedSize = encryptor.GetEncryptedSize(plaintext.Count);

        byte[] ciphertext = encryptor.Encrypt(plaintext, aad);
        Assert.Equal(expectedSize, ciphertext.Length);

        byte[] decipheredtext = encryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);

        Assert.Equal(plaintext.AsSpan(), decipheredtext.AsSpan());
    }
}
