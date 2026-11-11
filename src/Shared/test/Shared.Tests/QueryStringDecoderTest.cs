// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Internal;

public class QueryStringDecoderTest
{
    [Theory]
    [InlineData("", "")]
    [InlineData("value", "value")]
    [InlineData("+", " ")]
    [InlineData("a+b", "a b")]
    [InlineData("++a++", "  a  ")]
    [InlineData("%20", " ")]
    [InlineData("a%20b", "a b")]
    [InlineData("%2B", "+")]
    [InlineData("a+%2B+b", "a + b")]
    [InlineData("%C3%BC", "\u00FC")]
    [InlineData("%F0%9F%98%80", "\U0001F600")]
    [InlineData("a+%C3%BC%20b+%F0%9F%98%80c", "a \u00FC b \U0001F600c")]
    [InlineData("%", "%")]
    [InlineData("%2", "%2")]
    [InlineData("%zz+x", "%zz x")]
    [InlineData("%25", "%")]
    [InlineData("%2520", "%20")]
    public void DecodeToStringDecodesPlusAndPercentEncoding(string encoded, string expected)
    {
        Assert.Equal(expected, QueryStringDecoder.DecodeToString(encoded.AsMemory()));
    }

    [Theory]
    [InlineData("value")]
    [InlineData("a+b")]
    [InlineData("a%20b")]
    [InlineData("a+%2B%20b%")]
    [InlineData("%C3%BC+%F0%9F%98%80")]
    public void DecodeToStringMatchesExistingDecode(string encoded)
    {
        var count = 0;
        foreach (var pair in new QueryStringEnumerable("?" + encoded + "=" + encoded))
        {
            Assert.Equal(pair.DecodeName().ToString(), QueryStringDecoder.DecodeToString(pair.EncodedName));
            Assert.Equal(pair.DecodeValue().ToString(), QueryStringDecoder.DecodeToString(pair.EncodedValue));
            count++;
        }

        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(5000)]
    public void DecodeToStringDecodesLongValues(int length)
    {
        var encoded = "%2B" + new string('+', length - 7) + "a%20";
        var expected = "+" + new string(' ', length - 7) + "a ";

        Assert.Equal(length, encoded.Length);
        Assert.Equal(expected, QueryStringDecoder.DecodeToString(encoded.AsMemory()));
    }

    [Fact]
    public void DecodeToStringDecodesSliceOfLargerString()
    {
        var query = "x=a+b%20c&y=1";

        Assert.Equal("a b c", QueryStringDecoder.DecodeToString(query.AsMemory(2, 7)));
    }

    [Fact]
    public void DecodeToMemoryReturnsInputWhenNothingToDecode()
    {
        var query = "x=value&y=1";
        var encoded = query.AsMemory(2, 5);

        var decoded = QueryStringDecoder.DecodeToMemory(encoded);

        Assert.True(decoded.Equals(encoded));
    }

    [Theory]
    [InlineData("a+b", "a b")]
    [InlineData("a%20b", "a b")]
    [InlineData("a+%2B+b", "a + b")]
    [InlineData("%C3%BC+%F0%9F%98%80", "\u00FC \U0001F600")]
    [InlineData("%zz+x", "%zz x")]
    public void DecodeToMemoryDecodesPlusAndPercentEncoding(string encoded, string expected)
    {
        Assert.Equal(expected, QueryStringDecoder.DecodeToMemory(encoded.AsMemory()).ToString());
    }
}
