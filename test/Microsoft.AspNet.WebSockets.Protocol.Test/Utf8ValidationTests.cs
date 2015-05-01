// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Xunit;

namespace Microsoft.AspNet.WebSockets.Protocol.Test
{
    public class Utf8ValidationTests
    {
        [Theory]
        [InlineData(new byte[] { })]
        [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20, 0x57, 0x6F, 0x72, 0x6C, 0x64 })] // Hello World
        [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2D, 0xC2, 0xB5, 0x40, 0xC3, 0x9F, 0xC3, 0xB6, 0xC3, 0xA4, 0xC3, 0xBC, 0xC3, 0xA0, 0xC3, 0xA1 })] // "Hello-µ@ßöäüàá";
        // [InlineData(new byte[] { 0x68, 0x65, 0x6c, 0x6c, 0x6f, 0xf0, 0xa4, 0xad, 0xa2, 0x77, 0x6f, 0x72, 0x6c, 0x64 })] // "hello\U00024b62world"
        [InlineData(new byte[] { 0xf0, 0xa4, 0xad, 0xa2 })] // "\U00024b62"
        public void ValidateSingleValidSegments_Valid(byte[] data)
        {
            var state = new Utilities.Utf8MessageState();
            Assert.True(Utilities.TryValidateUtf8(new ArraySegment<byte>(data), endOfMessage: true, state: state));
        }

        [Theory]
        [InlineData(new byte[] { }, new byte[] { }, new byte[] { })]
        [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x20 }, new byte[] { }, new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 })] // Hello ,, World
        [InlineData(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x2D, 0xC2, }, new byte[] { 0xB5, 0x40, 0xC3, 0x9F, 0xC3, 0xB6, 0xC3, 0xA4, }, new byte[] { 0xC3, 0xBC, 0xC3, 0xA0, 0xC3, 0xA1 })] // "Hello-µ@ßöäüàá";
        public void ValidateMultipleValidSegments_Valid(byte[] data1, byte[] data2, byte[] data3)
        {
            var state = new Utilities.Utf8MessageState();
            Assert.True(Utilities.TryValidateUtf8(new ArraySegment<byte>(data1), endOfMessage: false, state: state));
            Assert.True(Utilities.TryValidateUtf8(new ArraySegment<byte>(data2), endOfMessage: false, state: state));
            Assert.True(Utilities.TryValidateUtf8(new ArraySegment<byte>(data3), endOfMessage: true, state: state));
        }

        [Theory]
        [InlineData(new byte[] { 0xfe })]
        [InlineData(new byte[] { 0xff })]
        [InlineData(new byte[] { 0xfe, 0xfe, 0xff, 0xff })]
        [InlineData(new byte[] { 0xc0, 0xb1 })] // Overlong Ascii
        [InlineData(new byte[] { 0xc1, 0xb1 })] // Overlong Ascii
        [InlineData(new byte[] { 0xe0, 0x80, 0xaf })] // Overlong
        [InlineData(new byte[] { 0xf0, 0x80, 0x80, 0xaf })] // Overlong
        [InlineData(new byte[] { 0xf8, 0x80, 0x80, 0x80, 0xaf })] // Overlong
        [InlineData(new byte[] { 0xfc, 0x80, 0x80, 0x80, 0x80, 0xaf })] // Overlong
        [InlineData(new byte[] { 0xed, 0xa0, 0x80, 0x65, 0x64, 0x69, 0x74, 0x65, 0x64 })] // 0xEDA080 decodes to 0xD800, which is a reserved high surrogate character.
        public void ValidateSingleInvalidSegment_Invalid(byte[] data)
        {
            var state = new Utilities.Utf8MessageState();
            Assert.False(Utilities.TryValidateUtf8(new ArraySegment<byte>(data), endOfMessage: true, state: state));
        }

        [Fact]
        public void ValidateIndividualInvalidSegments_Invalid()
        {
            var data = new byte[] { 0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5, 0xed, 0xa0, 0x80, 0x65, 0x64, 0x69, 0x74, 0x65, 0x64 };
            var state = new Utilities.Utf8MessageState();
            for (int i = 0; i < 12; i++)
            {
                Assert.True(Utilities.TryValidateUtf8(new ArraySegment<byte>(data, i, 1), endOfMessage: false, state: state), i.ToString());
            }
            Assert.False(Utilities.TryValidateUtf8(new ArraySegment<byte>(data, 12, 1), endOfMessage: false, state: state), 12.ToString());
        }

        [Fact]
        public void ValidateMultipleInvalidSegments_Invalid()
        {
            var data0 = new byte[] { 0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5, 0xf4 };
            var data1 = new byte[] { 0x90 };
            var state = new Utilities.Utf8MessageState();
            Assert.True(Utilities.TryValidateUtf8(new ArraySegment<byte>(data0), endOfMessage: false, state: state));
            Assert.False(Utilities.TryValidateUtf8(new ArraySegment<byte>(data1), endOfMessage: false, state: state));
        }
    }
}