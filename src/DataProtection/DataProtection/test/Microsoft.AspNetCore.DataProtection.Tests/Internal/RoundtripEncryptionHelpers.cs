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
    /// <see cref="IAuthenticatedEncryptor.TryEncrypt"/> and <see cref="IAuthenticatedEncryptor.TryDecrypt"/> APIs should do the same steps
    /// as <see cref="IAuthenticatedEncryptor.Encrypt"/> and <see cref="IAuthenticatedEncryptor.Decrypt"/> APIs.
    /// <br/>
    /// Method ensures that the two APIs are equivalent in terms of their behavior by performing a roundtrip encrypt-decrypt test.
    /// </summary>
    public static void AssertTryEncryptTryDecryptParity(IAuthenticatedEncryptor encryptor, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> aad)
        => AssertTryEncryptTryDecryptParity(encryptor, plaintext, aad);

    /// <summary>
    /// <see cref="IAuthenticatedEncryptor.TryEncrypt"/> and <see cref="IAuthenticatedEncryptor.TryDecrypt"/> APIs should do the same steps
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

        // assert calculated sizes are correct
        var expectedEncryptedSize = spanAuthenticatedEncryptor.GetEncryptedSize(plaintext.Count);
        Assert.Equal(expectedEncryptedSize, ciphertext.Length);
        var expectedDecryptedSize = spanAuthenticatedEncryptor.GetDecryptedSize(ciphertext.Length);
        Assert.Equal(expectedDecryptedSize, decipheredtext.Length);

        // perform TryEncrypt and Decrypt roundtrip - ensures cross operation compatibility
        var cipherTextPooled = ArrayPool<byte>.Shared.Rent(expectedEncryptedSize);
        try
        {
            var tryEncryptResult = spanAuthenticatedEncryptor.TryEncrypt(plaintext, aad, cipherTextPooled, out var bytesWritten);
            Assert.Equal(expectedEncryptedSize, bytesWritten);
            Assert.True(tryEncryptResult);

            var decipheredTryEncrypt = spanAuthenticatedEncryptor.Decrypt(new ArraySegment<byte>(cipherTextPooled, 0, expectedEncryptedSize), aad);
            Assert.Equal(plaintext.AsSpan(), decipheredTryEncrypt.AsSpan());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(cipherTextPooled);
        }

        // perform Encrypt and TryDecrypt roundtrip - ensures cross operation compatibility
        var plainTextPooled = ArrayPool<byte>.Shared.Rent(expectedDecryptedSize);
        try
        {
            var encrypted = spanAuthenticatedEncryptor.Encrypt(plaintext, aad);
            var decipheredTryDecrypt = spanAuthenticatedEncryptor.TryDecrypt(encrypted, aad, plainTextPooled, out var bytesWritten);
            Assert.Equal(plaintext.AsSpan(), plainTextPooled.AsSpan());
            Assert.True(decipheredTryDecrypt);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(cipherTextPooled);
        }
    }
}
