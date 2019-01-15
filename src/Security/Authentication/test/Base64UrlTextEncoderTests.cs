// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public class Base64UrlTextEncoderTests
    {
        [Fact]
        public void DataOfVariousLengthRoundTripCorrectly()
        {
            for (int length = 0; length != 256; ++length)
            {
                var data = new byte[length];
                for (int index = 0; index != length; ++index)
                {
                    data[index] = (byte)(5 + length + (index * 23));
                }
                string text = Base64UrlTextEncoder.Encode(data);
                byte[] result = Base64UrlTextEncoder.Decode(text);

                for (int index = 0; index != length; ++index)
                {
                    Assert.Equal(data[index], result[index]);
                }
            }
        }
    }
}
