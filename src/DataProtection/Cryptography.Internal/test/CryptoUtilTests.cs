// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.AspNetCore.Cryptography;

public unsafe class CryptoUtilTests
{
    [Fact]
    public void TimeConstantBuffersAreEqual_Array_Equal()
    {
        // Arrange
        byte[] a = new byte[] { 0x01, 0x23, 0x45, 0x67 };
        byte[] b = new byte[] { 0xAB, 0xCD, 0x23, 0x45, 0x67, 0xEF };

        // Act & assert
        Assert.True(CryptoUtil.TimeConstantBuffersAreEqual(a, 1, 3, b, 2, 3));
    }

    [Fact]
    public void TimeConstantBuffersAreEqual_Array_Unequal()
    {
        byte[] a = new byte[] { 0x01, 0x23, 0x45, 0x67 };
        byte[] b = new byte[] { 0xAB, 0xCD, 0x23, 0xFF, 0x67, 0xEF };

        // Act & assert
        Assert.False(CryptoUtil.TimeConstantBuffersAreEqual(a, 1, 3, b, 2, 3));
    }

    [Fact]
    public void TimeConstantBuffersAreEqual_Pointers_Equal()
    {
        // Arrange
        uint a = 0x01234567;
        uint b = 0x01234567;

        // Act & assert
        Assert.True(CryptoUtil.TimeConstantBuffersAreEqual((byte*)&a, (byte*)&b, sizeof(uint)));
    }

    [Fact]
    public void TimeConstantBuffersAreEqual_Pointers_Unequal()
    {
        // Arrange
        uint a = 0x01234567;
        uint b = 0x89ABCDEF;

        // Act & assert
        Assert.False(CryptoUtil.TimeConstantBuffersAreEqual((byte*)&a, (byte*)&b, sizeof(uint)));
    }
}
