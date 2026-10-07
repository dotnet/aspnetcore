// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.Managed;
using Microsoft.AspNetCore.DataProtection.Tests.Internal;
using Xunit;

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

        RoundtripEncryptionHelpers.AssertTryEncryptTryDecryptParity(encryptor, plaintext, aad);
    }

    [Fact]
    public void Constructor_PerformsSelfTest_ConsumesRandomBytes()
    {
        var genRandom = new SequentialGenRandom();
        byte initialValue = genRandom.CurrentValue;

        Secret kdk = new Secret(new byte[512 / 8]);
        _ = new AesGcmAuthenticatedEncryptor(kdk,
            derivedKeySizeInBytes: 256 / 8,
            genRandom: genRandom);

        // Indirectly testing that SelfTest ran by checking that random bytes were consumed
        Assert.NotEqual(initialValue, genRandom.CurrentValue);
    }
}

#endif
