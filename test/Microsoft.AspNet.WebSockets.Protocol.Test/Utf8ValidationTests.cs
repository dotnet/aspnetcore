// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
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
        // [InlineData(new byte[] { 0xc0, 0xaf })]
        // [InlineData(new byte[] { 0xe0, 0x80, 0xaf })]
        // [InlineData(new byte[] { 0xf4, 0x90, 0x80, 0x80 })]
        // [InlineData(new byte[] { 0xf0, 0x80, 0x80, 0xaf })]
        // [InlineData(new byte[] { 0xf8, 0x80, 0x80, 0x80, 0xaf })]
        // [InlineData(new byte[] { 0xfc, 0x80, 0x80, 0x80, 0x80, 0xaf })]
        // [InlineData(new byte[] { 0xc1, 0xbf })]
        // [InlineData(new byte[] { 0xed, 0xa0, 0x80, 0x65, 0x64, 0x69, 0x74, 0x65, 0x64 })] // 0xEDA080 decodes to 0xD800, which is a reserved high surrogate character.
        public void ValidateSingleInvalidSegment_Invalid(byte[] data)
        {
            var state = new Utilities.Utf8MessageState();
            Assert.False(Utilities.TryValidateUtf8(new ArraySegment<byte>(data), endOfMessage: true, state: state));
        }
        /*
        [Theory]
        // [InlineData(true, new byte[] { 0xce, 0xba, 0xe1, 0xbd, 0xb9, 0xcf, 0x83, 0xce, 0xbc, 0xce, 0xb5, 0xf4 }, false, new byte[] { 0x90 }, true, new byte[] { })]
        public void ValidateMultipleInvalidSegments_Invalid(bool valid1, byte[] data1, bool valid2, byte[] data2, bool valid3, byte[] data3)
        {
            var state = new Utilities.Utf8MessageState();
            Assert.True(valid1 == Utilities.TryValidateUtf8(new ArraySegment<byte>(data1), endOfMessage: false, state: state), "1st");
            Assert.True(valid2 == Utilities.TryValidateUtf8(new ArraySegment<byte>(data2), endOfMessage: false, state: state), "2nd");
            Assert.True(valid3 == Utilities.TryValidateUtf8(new ArraySegment<byte>(data3), endOfMessage: true, state: state), "3rd");
        }*/
    }
}