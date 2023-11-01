// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation.PBKDF2;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Cryptography.KeyDerivation;

public class Pbkdf2Tests
{
    // The 'numBytesRequested' parameters below are chosen to exercise code paths where
    // this value straddles the digest length of the PRF. We only use 5 iterations so
    // that our unit tests are fast.

    // This provider is only available in .NET Core because .NET Standard only supports HMACSHA1
    [Theory]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 - 1, "efmxNcKD/U1urTEDGvsThlPnHA==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 + 0, "efmxNcKD/U1urTEDGvsThlPnHDI=")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 + 1, "efmxNcKD/U1urTEDGvsThlPnHDLk")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 - 1, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRA==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 + 0, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRLo=")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 + 1, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRLpk")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 - 1, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm9")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 + 0, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm90Q==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 + 1, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm90Wk=")]
    public void RunTest_Normal_NetCore(string password, KeyDerivationPrf prf, int iterationCount, int numBytesRequested, string expectedValueAsBase64)
    {
        // Arrange
        byte[] salt = new byte[256];
        for (int i = 0; i < salt.Length; i++)
        {
            salt[i] = (byte)i;
        }

        // Act & assert
#if NETFRAMEWORK
        TestProvider<ManagedPbkdf2Provider>(password, salt, prf, iterationCount, numBytesRequested, expectedValueAsBase64);
#elif NETCOREAPP
        TestProvider<NetCorePbkdf2Provider>(password, salt, prf, iterationCount, numBytesRequested, expectedValueAsBase64);
#else
#error Update target frameworks
#endif
    }

    [Fact]
    public void RunTest_WithLongPassword_NetCore_FallbackToManaged()
    {
        // salt is less than 8 bytes
        byte[] salt = Encoding.UTF8.GetBytes("salt");
        const string expectedDerivedKeyBase64 = "Sc+V/c3fiZq5Z5qH3iavAiojTsW97FAp2eBNmCQAwCNzA8hfhFFYyQLIMK65qPnBFHOHXQPwAxNQNhaEAH9hzfiaNBSRJpF9V4rpl02d5ZpI6cZbsQFF7TJW7XJzQVpYoPDgJlg0xVmYLhn1E9qMtUVUuXsBjOOdd7K1M+ZI00c=";

#if NETFRAMEWORK
        RunTest_WithLongPassword_Impl<ManagedPbkdf2Provider>(salt, expectedDerivedKeyBase64);
#elif NETCOREAPP
        RunTest_WithLongPassword_Impl<NetCorePbkdf2Provider>(salt, expectedDerivedKeyBase64);
#else
#error Update target frameworks
#endif
    }

    [Fact]
    public void RunTest_WithLongPassword_NetCore()
    {
        // salt longer than 8 bytes
        var salt = Encoding.UTF8.GetBytes("abcdefghijkl");

#if NETFRAMEWORK
        RunTest_WithLongPassword_Impl<ManagedPbkdf2Provider>(salt, "NGJtFzYUaaSxu+3ZsMeZO5d/qPJDUYW4caLkFlaY0cLSYdh1PN4+nHUVp4pUUubJWu3UeXNMnHKNDfnn8GMfnDVrAGTv1lldszsvUJ0JQ6p4+daQEYBc//Tj/ejuB3luwW0IinyE7U/ViOQKbfi5pCZFMQ0FFx9I+eXRlyT+I74=");
#elif NETCOREAPP
        RunTest_WithLongPassword_Impl<NetCorePbkdf2Provider>(salt, "NGJtFzYUaaSxu+3ZsMeZO5d/qPJDUYW4caLkFlaY0cLSYdh1PN4+nHUVp4pUUubJWu3UeXNMnHKNDfnn8GMfnDVrAGTv1lldszsvUJ0JQ6p4+daQEYBc//Tj/ejuB3luwW0IinyE7U/ViOQKbfi5pCZFMQ0FFx9I+eXRlyT+I74=");
#else
#error Update target frameworks
#endif
    }

    // The 'numBytesRequested' parameters below are chosen to exercise code paths where
    // this value straddles the digest length of the PRF. We only use 5 iterations so
    // that our unit tests are fast.
    [Theory]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 - 1, "efmxNcKD/U1urTEDGvsThlPnHA==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 + 0, "efmxNcKD/U1urTEDGvsThlPnHDI=")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 + 1, "efmxNcKD/U1urTEDGvsThlPnHDLk")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 - 1, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRA==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 + 0, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRLo=")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 + 1, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRLpk")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 - 1, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm9")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 + 0, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm90Q==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 + 1, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm90Wk=")]
    public void RunTest_Normal_Managed(string password, KeyDerivationPrf prf, int iterationCount, int numBytesRequested, string expectedValueAsBase64)
    {
        // Arrange
        byte[] salt = new byte[256];
        for (int i = 0; i < salt.Length; i++)
        {
            salt[i] = (byte)i;
        }

        // Act & assert
        TestProvider<ManagedPbkdf2Provider>(password, salt, prf, iterationCount, numBytesRequested, expectedValueAsBase64);
    }

    // The 'numBytesRequested' parameters below are chosen to exercise code paths where
    // this value straddles the digest length of the PRF. We only use 5 iterations so
    // that our unit tests are fast.
    [ConditionalTheory]
    [ConditionalRunTestOnlyOnWindows]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 - 1, "efmxNcKD/U1urTEDGvsThlPnHA==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 + 0, "efmxNcKD/U1urTEDGvsThlPnHDI=")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 + 1, "efmxNcKD/U1urTEDGvsThlPnHDLk")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 - 1, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRA==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 + 0, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRLo=")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 + 1, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRLpk")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 - 1, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm9")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 + 0, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm90Q==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 + 1, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm90Wk=")]
    public void RunTest_Normal_Win7(string password, KeyDerivationPrf prf, int iterationCount, int numBytesRequested, string expectedValueAsBase64)
    {
        // Arrange
        byte[] salt = new byte[256];
        for (int i = 0; i < salt.Length; i++)
        {
            salt[i] = (byte)i;
        }

        // Act & assert
        TestProvider<Win7Pbkdf2Provider>(password, salt, prf, iterationCount, numBytesRequested, expectedValueAsBase64);
    }

    // The 'numBytesRequested' parameters below are chosen to exercise code paths where
    // this value straddles the digest length of the PRF. We only use 5 iterations so
    // that our unit tests are fast.
    [ConditionalTheory]
    [ConditionalRunTestOnlyOnWindows8OrLater]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 - 1, "efmxNcKD/U1urTEDGvsThlPnHA==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 + 0, "efmxNcKD/U1urTEDGvsThlPnHDI=")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA1, 5, 160 / 8 + 1, "efmxNcKD/U1urTEDGvsThlPnHDLk")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 - 1, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRA==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 + 0, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRLo=")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA256, 5, 256 / 8 + 1, "JRNz8bPKS02EG1vf7eWjA64IeeI+TI8gBEwb1oVvRLpk")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 - 1, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm9")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 + 0, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm90Q==")]
    [InlineData("my-password", KeyDerivationPrf.HMACSHA512, 5, 512 / 8 + 1, "ZTallQJrFn0279xIzaiA1XqatVTGei+ZjKngA7bIMtKMDUw6YJeGUQpFG8iGTgN+ri3LNDktNbzwfcSyZmm90Wk=")]
    public void RunTest_Normal_Win8(string password, KeyDerivationPrf prf, int iterationCount, int numBytesRequested, string expectedValueAsBase64)
    {
        // Arrange
        byte[] salt = new byte[256];
        for (int i = 0; i < salt.Length; i++)
        {
            salt[i] = (byte)i;
        }

        // Act & assert
        TestProvider<Win8Pbkdf2Provider>(password, salt, prf, iterationCount, numBytesRequested, expectedValueAsBase64);
    }

    [Fact]
    public void RunTest_WithLongPassword_Managed()
    {
        RunTest_WithLongPassword_Impl<ManagedPbkdf2Provider>();
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows]
    public void RunTest_WithLongPassword_Win7()
    {
        RunTest_WithLongPassword_Impl<Win7Pbkdf2Provider>();
    }

    [ConditionalFact]
    [ConditionalRunTestOnlyOnWindows8OrLater]
    public void RunTest_WithLongPassword_Win8()
    {
        RunTest_WithLongPassword_Impl<Win8Pbkdf2Provider>();
    }

    private static void RunTest_WithLongPassword_Impl<TProvider>()
        where TProvider : IPbkdf2Provider, new()
    {
        byte[] salt = Encoding.UTF8.GetBytes("salt");
        const string expectedDerivedKeyBase64 = "Sc+V/c3fiZq5Z5qH3iavAiojTsW97FAp2eBNmCQAwCNzA8hfhFFYyQLIMK65qPnBFHOHXQPwAxNQNhaEAH9hzfiaNBSRJpF9V4rpl02d5ZpI6cZbsQFF7TJW7XJzQVpYoPDgJlg0xVmYLhn1E9qMtUVUuXsBjOOdd7K1M+ZI00c=";
        RunTest_WithLongPassword_Impl<TProvider>(salt, expectedDerivedKeyBase64);
    }

    private static void RunTest_WithLongPassword_Impl<TProvider>(byte[] salt, string expectedDerivedKeyBase64)
        where TProvider : IPbkdf2Provider, new()
    {
        // Arrange
        string password = new String('x', 50000); // 50,000 char password
        const KeyDerivationPrf prf = KeyDerivationPrf.HMACSHA256;
        const int iterationCount = 5;
        const int numBytesRequested = 128;

        // Act & assert
        TestProvider<TProvider>(password, salt, prf, iterationCount, numBytesRequested, expectedDerivedKeyBase64);
    }

    private static void TestProvider<TProvider>(string password, byte[] salt, KeyDerivationPrf prf, int iterationCount, int numBytesRequested, string expectedDerivedKeyAsBase64)
        where TProvider : IPbkdf2Provider, new()
    {
        byte[] derivedKey = new TProvider().DeriveKey(password, salt, prf, iterationCount, numBytesRequested);
        Assert.Equal(numBytesRequested, derivedKey.Length);
        Assert.Equal(expectedDerivedKeyAsBase64, Convert.ToBase64String(derivedKey));
    }
}
