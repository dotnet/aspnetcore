// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Cryptography.SafeHandles;
using Xunit;

namespace Microsoft.AspNetCore.Cryptography;

public unsafe class UnsafeBufferUtilTests
{
    [Fact]
    public void BlockCopy_PtrToPtr_IntLength()
    {
        // Arrange
        long x = 0x0123456789ABCDEF;
        long y = 0;

        // Act
        UnsafeBufferUtil.BlockCopy(from: &x, to: &y, byteCount: (int)sizeof(long));

        // Assert
        Assert.Equal(x, y);
    }

    [Fact]
    public void BlockCopy_PtrToPtr_UIntLength()
    {
        // Arrange
        long x = 0x0123456789ABCDEF;
        long y = 0;

        // Act
        UnsafeBufferUtil.BlockCopy(from: &x, to: &y, byteCount: (uint)sizeof(long));

        // Assert
        Assert.Equal(x, y);
    }

    [Fact]
    public void BlockCopy_HandleToHandle()
    {
        // Arrange
        const string expected = "Hello there!";
        int cbExpected = expected.Length * sizeof(char);
        var controlHandle = LocalAlloc(cbExpected);
        for (int i = 0; i < expected.Length; i++)
        {
            ((char*)controlHandle.DangerousGetHandle())[i] = expected[i];
        }
        var testHandle = LocalAlloc(cbExpected);

        // Act
        UnsafeBufferUtil.BlockCopy(from: controlHandle, to: testHandle, length: (IntPtr)cbExpected);

        // Assert
        string actual = new string((char*)testHandle.DangerousGetHandle(), 0, expected.Length);
        GC.KeepAlive(testHandle);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BlockCopy_HandleToPtr()
    {
        // Arrange
        const string expected = "Hello there!";
        int cbExpected = expected.Length * sizeof(char);
        var controlHandle = LocalAlloc(cbExpected);
        for (int i = 0; i < expected.Length; i++)
        {
            ((char*)controlHandle.DangerousGetHandle())[i] = expected[i];
        }
        char* dest = stackalloc char[expected.Length];

        // Act
        UnsafeBufferUtil.BlockCopy(from: controlHandle, to: dest, byteCount: (uint)cbExpected);

        // Assert
        string actual = new string(dest, 0, expected.Length);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BlockCopy_PtrToHandle()
    {
        // Arrange
        const string expected = "Hello there!";
        int cbExpected = expected.Length * sizeof(char);
        var testHandle = LocalAlloc(cbExpected);

        // Act
        fixed (char* pExpected = expected)
        {
            UnsafeBufferUtil.BlockCopy(from: pExpected, to: testHandle, byteCount: (uint)cbExpected);
        }

        // Assert
        string actual = new string((char*)testHandle.DangerousGetHandle(), 0, expected.Length);
        GC.KeepAlive(testHandle);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SecureZeroMemory_IntLength()
    {
        // Arrange
        long x = 0x0123456789ABCDEF;

        // Act
        UnsafeBufferUtil.SecureZeroMemory((byte*)&x, byteCount: (int)sizeof(long));

        // Assert
        Assert.Equal(0, x);
    }

    [Fact]
    public void SecureZeroMemory_UIntLength()
    {
        // Arrange
        long x = 0x0123456789ABCDEF;

        // Act
        UnsafeBufferUtil.SecureZeroMemory((byte*)&x, byteCount: (uint)sizeof(long));

        // Assert
        Assert.Equal(0, x);
    }

    [Fact]
    public void SecureZeroMemory_ULongLength()
    {
        // Arrange
        long x = 0x0123456789ABCDEF;

        // Act
        UnsafeBufferUtil.SecureZeroMemory((byte*)&x, byteCount: (ulong)sizeof(long));

        // Assert
        Assert.Equal(0, x);
    }

    [Fact]
    public void SecureZeroMemory_IntPtrLength()
    {
        // Arrange
        long x = 0x0123456789ABCDEF;

        // Act
        UnsafeBufferUtil.SecureZeroMemory((byte*)&x, length: (IntPtr)sizeof(long));

        // Assert
        Assert.Equal(0, x);
    }

    private static LocalAllocHandle LocalAlloc(int cb)
    {
        return SecureLocalAllocHandle.Allocate((IntPtr)cb);
    }
}
