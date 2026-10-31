// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles;

public unsafe class SecureLocalAllocHandleTests
{
    [Fact]
    public void Duplicate_Copies_Data()
    {
        // Arrange
        const string expected = "xyz";
        int cbExpected = expected.Length * sizeof(char);
        var controlHandle = SecureLocalAllocHandle.Allocate((IntPtr)cbExpected);
        for (int i = 0; i < expected.Length; i++)
        {
            ((char*)controlHandle.DangerousGetHandle())[i] = expected[i];
        }

        // Act
        var duplicateHandle = controlHandle.Duplicate();

        // Assert
        Assert.Equal(expected, new string((char*)duplicateHandle.DangerousGetHandle(), 0, expected.Length)); // contents the same data
        Assert.NotEqual(controlHandle.DangerousGetHandle(), duplicateHandle.DangerousGetHandle()); // shouldn't just point to the same memory location
    }
}
