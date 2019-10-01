// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Cryptography.Cng
{
    public unsafe class BCryptUtilTests
    {
        [ConditionalFact]
        [ConditionalRunTestOnlyOnWindows]
        public void GenRandom_PopulatesBuffer()
        {
            // Arrange
            byte[] bytes = new byte[sizeof(Guid) + 6];
            bytes[0] = 0x04; // leading canary
            bytes[1] = 0x10;
            bytes[2] = 0xE4;
            bytes[sizeof(Guid) + 3] = 0xEA; // trailing canary
            bytes[sizeof(Guid) + 4] = 0xF2;
            bytes[sizeof(Guid) + 5] = 0x6A;

            fixed (byte* pBytes = &bytes[3])
            {
                for (int i = 0; i < 100; i++)
                {
                    // Act
                    BCryptUtil.GenRandom(pBytes, (uint)sizeof(Guid));

                    // Check that the canaries haven't changed
                    Assert.Equal(0x04, bytes[0]);
                    Assert.Equal(0x10, bytes[1]);
                    Assert.Equal(0xE4, bytes[2]);
                    Assert.Equal(0xEA, bytes[sizeof(Guid) + 3]);
                    Assert.Equal(0xF2, bytes[sizeof(Guid) + 4]);
                    Assert.Equal(0x6A, bytes[sizeof(Guid) + 5]);

                    // Check that the buffer was actually filled.
                    // This check will fail once every 2**128 runs, which is insignificant.
                    Guid newGuid = new Guid(bytes.Skip(3).Take(sizeof(Guid)).ToArray());
                    Assert.NotEqual(Guid.Empty, newGuid);

                    // Check that the first and last bytes of the buffer are not zero, which indicates that they
                    // were in fact filled. This check will fail around 0.8% of the time, so we'll iterate up
                    // to 100 times, which puts the total failure rate at once every 2**700 runs,
                    // which is insignificant.
                    if (bytes[3] != 0x00 && bytes[18] != 0x00)
                    {
                        return; // success!
                    }
                }
            }

            Assert.True(false, "Buffer was not filled as expected.");
        }
    }
}
