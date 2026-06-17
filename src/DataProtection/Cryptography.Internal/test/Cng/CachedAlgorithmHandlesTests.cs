// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Cryptography.Cng;

// This class tests both the properties and the output of hash algorithms.
// It only tests the properties of the encryption algorithms.
// Output of the encryption and key derivatoin functions are tested by other projects.
public unsafe class CachedAlgorithmHandlesTests
{
    private static readonly byte[] _dataToHash = Encoding.UTF8.GetBytes("Sample input data.");
    private static readonly byte[] _hmacKey = Encoding.UTF8.GetBytes("Secret key material.");

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void AES_CBC_Cached_Handle()
    {
        RunAesBlockCipherAlgorithmTest(() => CachedAlgorithmHandles.AES_CBC);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void AES_GCM_Cached_Handle()
    {
        RunAesBlockCipherAlgorithmTest(() => CachedAlgorithmHandles.AES_GCM);
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void SHA1_Cached_Handle_No_HMAC()
    {
        RunHashAlgorithmTest_No_HMAC(
            getter: () => CachedAlgorithmHandles.SHA1,
            expectedAlgorithmName: "SHA1",
            expectedBlockSizeInBytes: 512 / 8,
            expectedDigestSizeInBytes: 160 / 8,
            expectedDigest: "MbYo3dZmXtgUZcUoWoxkCDKFvkk=");
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void SHA1_Cached_Handle_With_HMAC()
    {
        RunHashAlgorithmTest_With_HMAC(
            getter: () => CachedAlgorithmHandles.HMAC_SHA1,
            expectedAlgorithmName: "SHA1",
            expectedBlockSizeInBytes: 512 / 8,
            expectedDigestSizeInBytes: 160 / 8,
            expectedDigest: "PjYTgLTWkt6NeH0NudIR7N47Ipg=");
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void SHA256_Cached_Handle_No_HMAC()
    {
        RunHashAlgorithmTest_No_HMAC(
            getter: () => CachedAlgorithmHandles.SHA256,
            expectedAlgorithmName: "SHA256",
            expectedBlockSizeInBytes: 512 / 8,
            expectedDigestSizeInBytes: 256 / 8,
            expectedDigest: "5uRfQadsrnUTa3/TEo5PP6SDZQkb9AcE4wNXDVcM0Fo=");
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void SHA256_Cached_Handle_With_HMAC()
    {
        RunHashAlgorithmTest_With_HMAC(
            getter: () => CachedAlgorithmHandles.HMAC_SHA256,
            expectedAlgorithmName: "SHA256",
            expectedBlockSizeInBytes: 512 / 8,
            expectedDigestSizeInBytes: 256 / 8,
            expectedDigest: "KLzo0lVg5gZkpL5D6Ck7QT8w4iuPCe/pGCrMcOXWbKY=");
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void SHA512_Cached_Handle_No_HMAC()
    {
        RunHashAlgorithmTest_No_HMAC(
            getter: () => CachedAlgorithmHandles.SHA512,
            expectedAlgorithmName: "SHA512",
            expectedBlockSizeInBytes: 1024 / 8,
            expectedDigestSizeInBytes: 512 / 8,
            expectedDigest: "jKI7WrcgPP7n2HAYOb8uFRi7xEsNG/BmdGd18dwwkIpqJ4Vmlk2b+8hssLyMQlprTSKVJNObSiYUqW5THS7okw==");
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void SHA512_Cached_Handle_With_HMAC()
    {
        RunHashAlgorithmTest_With_HMAC(
            getter: () => CachedAlgorithmHandles.HMAC_SHA512,
            expectedAlgorithmName: "SHA512",
            expectedBlockSizeInBytes: 1024 / 8,
            expectedDigestSizeInBytes: 512 / 8,
            expectedDigest: "pKTX5vtPtbsn7pX9ISDlOYr1NFklTBIPYAFICy0ZQbFc0QVzGaTUvtqTOi91I0sHa1DIod6uIogux5iLdHjfcA==");
    }

    private static void RunAesBlockCipherAlgorithmTest(Func<BCryptAlgorithmHandle> getter)
    {
        // Getter must return the same instance of the cached handle
        var algorithmHandle = getter();
        var algorithmHandleSecondAttempt = getter();
        Assert.NotNull(algorithmHandle);
        Assert.Same(algorithmHandle, algorithmHandleSecondAttempt);

        // Validate that properties are what we expect
        Assert.Equal("AES", algorithmHandle.GetAlgorithmName());
        Assert.Equal((uint)(128 / 8), algorithmHandle.GetCipherBlockLength());
        var supportedKeyLengths = algorithmHandle.GetSupportedKeyLengths();
        Assert.Equal(128U, supportedKeyLengths.dwMinLength);
        Assert.Equal(256U, supportedKeyLengths.dwMaxLength);
        Assert.Equal(64U, supportedKeyLengths.dwIncrement);
    }

    private static void RunHashAlgorithmTest_No_HMAC(
        Func<BCryptAlgorithmHandle> getter,
        string expectedAlgorithmName,
        uint expectedBlockSizeInBytes,
        uint expectedDigestSizeInBytes,
        string expectedDigest)
    {
        // Getter must return the same instance of the cached handle
        var algorithmHandle = getter();
        var algorithmHandleSecondAttempt = getter();
        Assert.NotNull(algorithmHandle);
        Assert.Same(algorithmHandle, algorithmHandleSecondAttempt);

        // Validate that properties are what we expect
        Assert.Equal(expectedAlgorithmName, algorithmHandle.GetAlgorithmName());
        Assert.Equal(expectedBlockSizeInBytes, algorithmHandle.GetHashBlockLength());
        Assert.Equal(expectedDigestSizeInBytes, algorithmHandle.GetHashDigestLength());

        // Perform the digest calculation and validate against our expectation
        var hashHandle = algorithmHandle.CreateHash();
        byte[] outputHash = new byte[expectedDigestSizeInBytes];
        fixed (byte* pInput = _dataToHash)
        {
            fixed (byte* pOutput = outputHash)
            {
                hashHandle.HashData(pInput, (uint)_dataToHash.Length, pOutput, (uint)outputHash.Length);
            }
        }
        Assert.Equal(expectedDigest, Convert.ToBase64String(outputHash));
    }

    private static void RunHashAlgorithmTest_With_HMAC(
       Func<BCryptAlgorithmHandle> getter,
       string expectedAlgorithmName,
       uint expectedBlockSizeInBytes,
       uint expectedDigestSizeInBytes,
       string expectedDigest)
    {
        // Getter must return the same instance of the cached handle
        var algorithmHandle = getter();
        var algorithmHandleSecondAttempt = getter();
        Assert.NotNull(algorithmHandle);
        Assert.Same(algorithmHandle, algorithmHandleSecondAttempt);

        // Validate that properties are what we expect
        Assert.Equal(expectedAlgorithmName, algorithmHandle.GetAlgorithmName());
        Assert.Equal(expectedBlockSizeInBytes, algorithmHandle.GetHashBlockLength());
        Assert.Equal(expectedDigestSizeInBytes, algorithmHandle.GetHashDigestLength());

        // Perform the digest calculation and validate against our expectation
        fixed (byte* pKey = _hmacKey)
        {
            var hashHandle = algorithmHandle.CreateHmac(pKey, (uint)_hmacKey.Length);
            byte[] outputHash = new byte[expectedDigestSizeInBytes];
            fixed (byte* pInput = _dataToHash)
            {
                fixed (byte* pOutput = outputHash)
                {
                    hashHandle.HashData(pInput, (uint)_dataToHash.Length, pOutput, (uint)outputHash.Length);
                }
            }
            Assert.Equal(expectedDigest, Convert.ToBase64String(outputHash));
        }
    }
}
