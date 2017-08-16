// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class IntegerDecoderTests
    {
        [Theory]
        [MemberData(nameof(IntegerData))]
        public void IntegerDecode(int i, int prefixLength, byte[] octets)
        {
            var decoder = new IntegerDecoder();
            var result = decoder.BeginDecode(octets[0], prefixLength);

            if (octets.Length == 1)
            {
                Assert.True(result);
            }
            else
            {
                var j = 1;

                for (; j < octets.Length - 1; j++)
                {
                    Assert.False(decoder.Decode(octets[j]));
                }

                Assert.True(decoder.Decode(octets[j]));
            }

            Assert.Equal(i, decoder.Value);
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
