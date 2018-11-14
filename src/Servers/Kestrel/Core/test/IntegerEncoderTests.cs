// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class IntegerEncoderTests
    {
        [Theory]
        [MemberData(nameof(IntegerData))]
        public void IntegerEncode(int i, int prefixLength, byte[] expectedOctets)
        {
            var buffer = new byte[expectedOctets.Length];

            Assert.True(IntegerEncoder.Encode(i, prefixLength, buffer, out var octets));
            Assert.Equal(expectedOctets.Length, octets);
            Assert.Equal(expectedOctets, buffer);
        }

        public static TheoryData<int, int, byte[]> IntegerData
        {
            get
            {
                var data = new TheoryData<int, int, byte[]>();

                data.Add(10, 5, new byte[] { 10 });
                data.Add(1337, 5, new byte[] { 0x1f, 0x9a, 0x0a });
                data.Add(42, 8, new byte[] { 42 });

                return data;
            }
        }
    }
}
