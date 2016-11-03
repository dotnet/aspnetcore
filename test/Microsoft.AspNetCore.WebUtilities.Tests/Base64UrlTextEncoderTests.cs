// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.WebUtilities
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

        [Theory]
        [InlineData("", "")]
        [InlineData("+", "-")]
        [InlineData("/", "_")]
        [InlineData("=", "")]
        [InlineData("==", "")]
        [InlineData("a+b+c+==", "a-b-c-")]
        [InlineData("a/b/c==", "a_b_c")]
        [InlineData("a+b/c==", "a-b_c")]
        [InlineData("a+b/c", "a-b_c")]
        [InlineData("abcd", "abcd")]
        public void EncodeInternal_Replaces_UrlEncodableCharacters(string base64EncodedValue, string expectedValue)
        {
            // Arrange & Act
            var result = Base64UrlTextEncoder.EncodeInternal(base64EncodedValue);

            // Assert
            Assert.Equal(expectedValue, result);
        }

        [Theory]
        [InlineData("_", "/===")]
        [InlineData("-", "+===")]
        [InlineData("a-b-c", "a+b+c===")]
        [InlineData("a_b_c_d", "a/b/c/d=")]
        [InlineData("a-b_c", "a+b/c===")]
        [InlineData("a-b_c-d", "a+b/c+d=")]
        [InlineData("a-b_c", "a+b/c===")]
        [InlineData("abcd", "abcd")]
        public void DecodeToBase64String_ReturnsValid_Base64String(string text, string expectedValue)
        {
            // Arrange & Act
            var actual = Base64UrlTextEncoder.DecodeToBase64String(text);

            // Assert
            Assert.Equal(expectedValue, actual);
        }
    }
}