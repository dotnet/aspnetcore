// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Cryptography.Cng;

public unsafe class BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO_Tests
{
    [Fact]
    public void Init_SetsProperties()
    {
        // Act
        BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Init(out var cipherModeInfo);

        // Assert
        Assert.Equal((uint)sizeof(BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO), cipherModeInfo.cbSize);
        Assert.Equal(1U, cipherModeInfo.dwInfoVersion);
        Assert.Equal(IntPtr.Zero, (IntPtr)cipherModeInfo.pbNonce);
        Assert.Equal(0U, cipherModeInfo.cbNonce);
        Assert.Equal(IntPtr.Zero, (IntPtr)cipherModeInfo.pbAuthData);
        Assert.Equal(0U, cipherModeInfo.cbAuthData);
        Assert.Equal(IntPtr.Zero, (IntPtr)cipherModeInfo.pbTag);
        Assert.Equal(0U, cipherModeInfo.cbTag);
        Assert.Equal(IntPtr.Zero, (IntPtr)cipherModeInfo.pbMacContext);
        Assert.Equal(0U, cipherModeInfo.cbMacContext);
        Assert.Equal(0U, cipherModeInfo.cbAAD);
        Assert.Equal(0UL, cipherModeInfo.cbData);
        Assert.Equal(0U, cipherModeInfo.dwFlags);
    }
}
