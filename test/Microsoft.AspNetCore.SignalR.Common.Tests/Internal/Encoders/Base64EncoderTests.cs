// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var encodedMessage = Encoding.UTF8.GetBytes(encoded);
            var decodedMessage = Encoding.UTF8.GetString(new Base64Encoder().Decode(encodedMessage).ToArray());
            Assert.Equal(payload, decodedMessage);
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
