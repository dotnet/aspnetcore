// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var result = decoder.BeginTryDecode(octets[0], prefixLength, out var intResult);

            if (octets.Length == 1)
            {
                Assert.True(result);
            }
            else
            {
                var j = 1;

                for (; j < octets.Length - 1; j++)
                {
                    Assert.False(decoder.TryDecode(octets[j], out intResult));
                }

                Assert.True(decoder.TryDecode(octets[j], out intResult));
            }

            Assert.Equal(i, intResult);
        }

        [Theory]
        [MemberData(nameof(IntegerData_OverMax))]
        public void IntegerDecode_Throws_IfMaxExceeded(int prefixLength, byte[] octets)
        {
            var decoder = new IntegerDecoder();
            var result = decoder.BeginTryDecode(octets[0], prefixLength, out var intResult);

            for (var j = 1; j < octets.Length - 1; j++)
            {
                Assert.False(decoder.TryDecode(octets[j], out intResult));
            }

            Assert.Throws<HPackDecodingException>(() => decoder.TryDecode(octets[octets.Length - 1], out intResult));
        }

        public static TheoryData<int, int, byte[]> IntegerData
        {
            get
            {
                var data = new TheoryData<int, int, byte[]>();

                data.Add(10, 5, new byte[] { 10 });
                data.Add(1337, 5, new byte[] { 0x1f, 0x9a, 0x0a });
                data.Add(42, 8, new byte[] { 42 });
                data.Add(7, 3, new byte[] { 0x7, 0x0 });
                data.Add(int.MaxValue, 1, new byte[] { 0x01, 0xfe, 0xff, 0xff, 0xff, 0x07 });
                data.Add(int.MaxValue, 8, new byte[] { 0xff, 0x80, 0xfe, 0xff, 0xff, 0x07 });

                return data;
            }
        }

        public static TheoryData<int, byte[]> IntegerData_OverMax
        {
            get
            {
                var data = new TheoryData<int, byte[]>();

                data.Add(1, new byte[] { 0x01, 0xff, 0xff, 0xff, 0xff, 0x07 }); // Int32.MaxValue + 1
                data.Add(1, new byte[] { 0x01, 0xff, 0xff, 0xff, 0xff, 0x08 }); // MSB exceeds maximum
                data.Add(1, new byte[] { 0x01, 0xff, 0xff, 0xff, 0xff, 0x80 }); // Undefined since continuation bit set
                data.Add(8, new byte[] { 0xff, 0x81, 0xfe, 0xff, 0xff, 0x07 }); // Int32.MaxValue + 1
                data.Add(8, new byte[] { 0xff, 0x81, 0xfe, 0xff, 0xff, 0x08 }); // MSB exceeds maximum
                data.Add(8, new byte[] { 0xff, 0x81, 0xfe, 0xff, 0xff, 0x80 }); // Undefined since continuation bit set

                return data;
            }
        }
    }
}
