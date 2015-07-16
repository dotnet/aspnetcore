// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication
{
    public class Base64UrlTextEncoderTests
    {
        [Fact]
        public void DataOfVariousLengthRoundTripCorrectly()
        {
            var encoder = new Base64UrlTextEncoder();
            for (int length = 0; length != 256; ++length)
            {
                var data = new byte[length];
                for (int index = 0; index != length; ++index)
                {
                    data[index] = (byte)(5 + length + (index * 23));
                }
                string text = encoder.Encode(data);
                byte[] result = encoder.Decode(text);

                for (int index = 0; index != length; ++index)
                {
                    result[index].ShouldBe(data[index]);
                }
            }
        }
    }
}
