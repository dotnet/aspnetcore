// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection;

/// <summary>
/// Helpful ISecret-based assertions.
/// </summary>
public static class SecretAssert
{
    /// <summary>
    /// Asserts that two <see cref="ISecret"/> instances contain the same material.
    /// </summary>
    public static void Equal(ISecret secret1, ISecret secret2)
    {
        Assert.Equal(SecretToBase64String(secret1), SecretToBase64String(secret2));
    }

    /// <summary>
    /// Asserts that <paramref name="secret"/> has the length specified by <paramref name="expectedLengthInBits"/>.
    /// </summary>
    public static void LengthIs(int expectedLengthInBits, ISecret secret)
    {
        Assert.Equal(expectedLengthInBits, checked(secret.Length * 8));
    }

    /// <summary>
    /// Asserts that two <see cref="ISecret"/> instances do not contain the same material.
    /// </summary>
    public static void NotEqual(ISecret secret1, ISecret secret2)
    {
        Assert.NotEqual(SecretToBase64String(secret1), SecretToBase64String(secret2));
    }

    private static string SecretToBase64String(ISecret secret)
    {
        byte[] secretBytes = new byte[secret.Length];
        secret.WriteSecretIntoBuffer(new ArraySegment<byte>(secretBytes));
        return Convert.ToBase64String(secretBytes);
    }
}
