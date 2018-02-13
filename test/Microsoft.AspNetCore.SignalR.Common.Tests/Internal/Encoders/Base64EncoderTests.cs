// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Internal.Encoders
{
    public class Base64EncoderTests
    {
        [Theory]
        [MemberData(nameof(Payloads))]
        public void VerifyDecode(string payload, string encoded)
        {
            var message = Encoding.UTF8.GetBytes(payload);
            var encodedMessage = Encoding.UTF8.GetString(new Base64Encoder().Encode(message));
            Assert.Equal(encoded, encodedMessage);
        }

        [Theory]
        [MemberData(nameof(Payloads))]
        public void VerifyEncode(string payload, string encoded)
        {
            ReadOnlySpan<byte> encodedMessage = Encoding.UTF8.GetBytes(encoded);
            var encoder = new Base64Encoder();
            encoder.TryDecode(ref encodedMessage, out var data);
            var decodedMessage = Encoding.UTF8.GetString(data.ToArray());
            Assert.Equal(payload, decodedMessage);
        }

        [Fact]
        public void CanParseMultipleMessages()
        {
            ReadOnlySpan<byte> data = Encoding.UTF8.GetBytes("28:QQpSDUMNCjtERUYxMjM0NTY3ODkw;4:QUJD;4:QUJD;");
            var encoder = new Base64Encoder();
            Assert.True(encoder.TryDecode(ref data, out var payload1));
            Assert.True(encoder.TryDecode(ref data, out var payload2));
            Assert.True(encoder.TryDecode(ref data, out var payload3));
            Assert.False(encoder.TryDecode(ref data, out var payload4));
            Assert.Equal(0, data.Length);
            var payload1Value = Encoding.UTF8.GetString(payload1.ToArray());
            var payload2Value = Encoding.UTF8.GetString(payload2.ToArray());
            var payload3Value = Encoding.UTF8.GetString(payload3.ToArray());
            Assert.Equal("A\nR\rC\r\n;DEF1234567890", payload1Value);
            Assert.Equal("ABC", payload2Value);
            Assert.Equal("ABC", payload3Value);
        }

        public static IEnumerable<object[]> Payloads =>
            new object[][]
            {
                new object[] { "", "0:;" },
                new object[] { "ABC", "4:QUJD;" },
                new object[] { "A\nR\rC\r\n;DEF1234567890", "28:QQpSDUMNCjtERUYxMjM0NTY3ODkw;" },
            };
    }
}
