// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Cryptography.SafeHandles
{
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
}
