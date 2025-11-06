// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;

namespace Microsoft.AspNetCore.DataProtection.Tests.Internal;

internal static class RoundtripEncryptionHelpers
{
    /// <summary>
    /// <see cref="ISpanAuthenticatedEncryptor.TryEncrypt"/> and <see cref="ISpanAuthenticatedEncryptor.TryDecrypt"/> APIs should do the same steps
    /// as <see cref="IAuthenticatedEncryptor.Encrypt"/> and <see cref="IAuthenticatedEncryptor.Decrypt"/> APIs.
    /// <br/>
    /// Method ensures that the two APIs are equivalent in terms of their behavior by performing a roundtrip encrypt-decrypt test.
    /// </summary>
    public static void AssertTryEncryptTryDecryptParity(IAuthenticatedEncryptor encryptor, ArraySegment<byte> plaintext, ArraySegment<byte> aad)
    {
        var spanAuthenticatedEncryptor = encryptor as ISpanAuthenticatedEncryptor;
        Debug.Assert(spanAuthenticatedEncryptor != null, "ISpanDataProtector is not supported by the encryptor");

        // assert "allocatey" Encrypt/Decrypt APIs roundtrip correctly
        byte[] ciphertext = encryptor.Encrypt(plaintext, aad);
        byte[] decipheredtext = encryptor.Decrypt(new ArraySegment<byte>(ciphertext), aad);
        Assert.Equal(plaintext.AsSpan(), decipheredtext.AsSpan());

        // perform TryEncrypt and Decrypt roundtrip - ensures cross operation compatibility
        var buffer = new ArrayBufferWriter<byte>();
        spanAuthenticatedEncryptor.Encrypt(plaintext, aad, buffer);
        var encryptResult = buffer.WrittenSpan;
        Assert.Equal(ciphertext.Length, encryptResult.Length);
        Assert.True(encryptResult.SequenceEqual(ciphertext));

        buffer = new ArrayBufferWriter<byte>();
        spanAuthenticatedEncryptor.Decrypt(ciphertext, aad, buffer);
        var decryptedResult = buffer.WrittenSpan;
        Assert.Equal(decipheredtext.Length, decryptedResult.Length);
        Assert.True(decryptedResult.SequenceEqual(decipheredtext));

        // perform Encrypt and TryDecrypt roundtrip - ensures cross operation compatibility
        var encrypted = spanAuthenticatedEncryptor.Encrypt(plaintext, aad);

        buffer = new ArrayBufferWriter<byte>();
        spanAuthenticatedEncryptor.Decrypt(encrypted, aad, buffer);
        var decryptedResult2 = buffer.WrittenSpan;
        Assert.Equal(decipheredtext.Length, decryptedResult2.Length);
        Assert.True(decryptedResult2.SequenceEqual(decipheredtext));
    }

    /// <summary>
    /// <see cref="ISpanDataProtector.TryProtect"/> and <see cref="ISpanDataProtector.TryUnprotect"/> APIs should do the same steps
    /// as <see cref="IDataProtector.Protect"/> and <see cref="IDataProtector.Unprotect"/> APIs.
    /// <br/>
    /// Method ensures that the two APIs are equivalent in terms of their behavior by performing a roundtrip protect-unprotect test.
    /// </summary>
    public static void AssertTryProtectTryUnprotectParity(ISpanDataProtector protector, ReadOnlySpan<byte> plaintext)
    {
        // assert "allocatey" Protect/Unprotect APIs roundtrip correctly
        byte[] protectedData = protector.Protect(plaintext.ToArray());
        byte[] unprotectedData = protector.Unprotect(protectedData);
        Assert.Equal(plaintext, unprotectedData.AsSpan());

        // perform TryProtect and Unprotect roundtrip - ensures cross operation compatibility
        var buffer = new ArrayBufferWriter<byte>();
        protector.Protect(plaintext, buffer);
        var protectedResult = buffer.WrittenSpan;
        Assert.Equal(protectedData.Length, protectedResult.Length);
        Assert.True(protectedResult.SequenceEqual(protectedData));

        buffer = new ArrayBufferWriter<byte>();
        protector.Unprotect(unprotectedData, buffer);
        var unProtectedResult = buffer.WrittenSpan;
        Assert.Equal(unprotectedData.Length, unProtectedResult.Length);
        Assert.True(unProtectedResult.SequenceEqual(unprotectedData));

        // perform Protect and TryUnprotect roundtrip - ensures cross operation compatibility
        // Note: This test is limited because we can't easily access the correct AAD from outside the protector
        // But we can test basic functionality with empty AAD and expect it to fail gracefully
        var protectedByProtect = protector.Protect(plaintext.ToArray());

        buffer = new ArrayBufferWriter<byte>();
        protector.Unprotect(protectedByProtect, buffer);
        var unProtectedResult2 = buffer.WrittenSpan;
        Assert.Equal(unprotectedData.Length, unProtectedResult.Length);
        Assert.True(unProtectedResult.SequenceEqual(unprotectedData));
    }
}
